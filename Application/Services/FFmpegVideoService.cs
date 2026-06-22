using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Entity;
using Core.Entity.Models;
using Core.Enums;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Application.Services
{
    public class FFmpegVideoService : IVideoRendererService
    {
        private const int MaxVisualsPerRenderChunk = 24;
        private const int MaxCommandLengthBeforeChunking = 22000;

        private readonly string _assetsPath;

        public FFmpegVideoService()
        {
            _assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets");
        }

        public async Task<string> RenderVideoAsync(
            SceneLayoutStagePayload layout,
            string outputPath,
            string cultureCode = "en-US",
            CancellationToken ct = default,
            Func<string, Task>? logAsync = null)
        {
            var tempId = Guid.NewGuid().ToString("N")[..8];
            var workDir = Path.GetDirectoryName(outputPath)!;

            var srtPath = Path.Combine(workDir, $"subs_{tempId}.ass");
            var audioListPath = Path.Combine(workDir, $"audio_{tempId}.txt");
            var fontsDir = Path.Combine(workDir, $"fonts_{tempId}");

            NormalizeLayoutMediaPaths(layout, workDir);

            // =================================================================
            // 🔥 FONT OPERASYONU (DEDEKTİF MODU)
            // =================================================================
            // Veritabanından gelen isim: "Poppins-Bold" (veya GUID)
            string requestedFontName = layout.Style.CaptionSettings.FontName;

            // 1. Akıllı Arama: Dosya adı GUID bile olsa, içini okuyup Aile Adı "Poppins" olanı bulur.
            string originalFontPath = GetSmartFontPath(requestedFontName);

            // 2. Fontun özelliklerini (Bold/Italic) isimden ayrıştır
            var fontInfo = ParseFontInfo(requestedFontName);

            // 3. Dosyayı temiz bir isimle (örn: Poppins.ttf) workDir'e kopyala
            // Böylece ASS dosyası "Poppins" aradığında, yanındaki dosyayı bulacak.
            Directory.CreateDirectory(fontsDir);
            string cleanFileName = $"{fontInfo.FamilyName}.ttf";
            string localFontPath = Path.Combine(fontsDir, cleanFileName);

            if (File.Exists(originalFontPath) && !File.Exists(localFontPath))
            {
                File.Copy(originalFontPath, localFontPath);
            }
            else if (!File.Exists(originalFontPath))
            {
                // Font hiç bulunamazsa, Arial kullanması için fallback yapabiliriz
                // veya loglayabiliriz.
                fontInfo.FamilyName = "Arial";
            }

            // 4. Layout ayarını güncelle ki ASS oluştururken doğru isim kullanılsın
            layout.Style.CaptionSettings.FontName = fontInfo.FamilyName;
            // =================================================================

            var musicPath = GetRandomMusicFile();

            try
            {
                var renderInChunks = ShouldRenderInChunks(layout, srtPath, audioListPath, musicPath, outputPath, fontsDir);
                if (renderInChunks)
                {
                    await SafeLogAsync(logAsync, PipelineLiveLog.Warning("FFmpeg komutu uzun/yoğun görünüyor. Render parçalara bölünerek alınacak."));
                    await RenderChunkedAsync(layout, workDir, tempId, musicPath, outputPath, fontsDir, cultureCode, fontInfo, ct, logAsync);
                }
                else
                {
                    await SafeLogAsync(logAsync, "FFmpeg tek geçiş render modu kullanılacak.");
                    await RenderSinglePassAsync(layout, srtPath, audioListPath, musicPath, outputPath, fontsDir, cultureCode, fontInfo, ct, logAsync);
                }

                return outputPath;
            }
            finally
            {
                if (File.Exists(srtPath)) File.Delete(srtPath);
                if (File.Exists(audioListPath)) File.Delete(audioListPath);
                if (Directory.Exists(fontsDir)) Directory.Delete(fontsDir, recursive: true);
            }
        }

        private bool ShouldRenderInChunks(
            SceneLayoutStagePayload layout,
            string subPath,
            string audioListPath,
            string? musicPath,
            string outputPath,
            string fontsDir)
        {
            if (layout.VisualTrack.Count > MaxVisualsPerRenderChunk) return true;

            var args = BuildFFmpegCommand(layout, subPath, audioListPath, musicPath, outputPath, fontsDir, preferHardwareEncoder: true);
            return args.Length > MaxCommandLengthBeforeChunking;
        }

        private async Task RenderSinglePassAsync(
            SceneLayoutStagePayload layout,
            string subPath,
            string audioListPath,
            string? musicPath,
            string outputPath,
            string fontsDir,
            string cultureCode,
            FontInfo fontInfo,
            CancellationToken ct,
            Func<string, Task>? logAsync)
        {
            await GenerateDynamicAssFileAsync(layout.CaptionTrack, subPath, layout.Style.CaptionSettings, cultureCode, fontInfo, layout.Width, layout.Height);
            await GenerateAudioListAsync(layout.AudioTrack, audioListPath);
            await RunFFmpegWithFallbackAsync(layout, subPath, audioListPath, musicPath, outputPath, fontsDir, ct, logAsync);
        }

        private async Task RenderChunkedAsync(
            SceneLayoutStagePayload layout,
            string workDir,
            string tempId,
            string? musicPath,
            string outputPath,
            string fontsDir,
            string cultureCode,
            FontInfo fontInfo,
            CancellationToken ct,
            Func<string, Task>? logAsync)
        {
            var chunkDir = Path.Combine(workDir, $"chunks_{tempId}");
            Directory.CreateDirectory(chunkDir);

            var chunkFiles = new List<string>();

            try
            {
                var orderedVisuals = layout.VisualTrack
                    .OrderBy(v => v.StartTime)
                    .ThenBy(v => v.SceneIndex)
                    .ToList();

                var totalChunks = (int)Math.Ceiling(orderedVisuals.Count / (double)MaxVisualsPerRenderChunk);
                await SafeLogAsync(logAsync, $"Parçalı render başladı. Toplam parça: {totalChunks}, görsel vuruş: {orderedVisuals.Count}.");

                var chunkIndex = 0;
                for (var i = 0; i < orderedVisuals.Count; i += MaxVisualsPerRenderChunk)
                {
                    var chunkVisuals = orderedVisuals
                        .Skip(i)
                        .Take(MaxVisualsPerRenderChunk)
                        .ToList();

                    if (chunkVisuals.Count == 0) continue;

                    chunkIndex++;
                    var chunkStart = chunkVisuals.Min(v => v.StartTime);
                    var chunkEnd = chunkVisuals.Max(v => v.StartTime + v.Duration);
                    var chunkLayout = BuildChunkLayout(layout, chunkVisuals, chunkStart, chunkEnd);

                    var chunkSubPath = Path.Combine(chunkDir, $"subs_{chunkIndex:000}.ass");
                    var chunkAudioListPath = Path.Combine(chunkDir, $"audio_{chunkIndex:000}.txt");
                    var chunkOutputPath = Path.Combine(chunkDir, $"chunk_{chunkIndex:000}.mp4");

                    await SafeLogAsync(logAsync, $"Render parçası işleniyor: {chunkIndex}/{totalChunks}. Süre: {chunkLayout.TotalDuration:F1} sn, görsel vuruş: {chunkLayout.VisualTrack.Count}.");
                    await RenderSinglePassAsync(chunkLayout, chunkSubPath, chunkAudioListPath, musicPath, chunkOutputPath, fontsDir, cultureCode, fontInfo, ct, logAsync);
                    chunkFiles.Add(chunkOutputPath);
                }

                if (chunkFiles.Count == 0)
                    throw new InvalidOperationException("Chunk render icin sahne bulunamadi.");

                if (chunkFiles.Count == 1)
                {
                    await SafeLogAsync(logAsync, PipelineLiveLog.Success("Tek render parçası üretildi. Final dosyaya kopyalanıyor."));
                    File.Copy(chunkFiles[0], outputPath, overwrite: true);
                    return;
                }

                await ConcatenateChunksAsync(chunkFiles, outputPath, chunkDir, ct, logAsync);
            }
            finally
            {
                if (Directory.Exists(chunkDir)) Directory.Delete(chunkDir, recursive: true);
            }
        }

        private static SceneLayoutStagePayload BuildChunkLayout(
            SceneLayoutStagePayload source,
            List<VisualEvent> chunkVisuals,
            double chunkStart,
            double chunkEnd)
        {
            var chunkSceneIndexes = chunkVisuals.Select(v => v.SceneIndex).ToHashSet();

            return new SceneLayoutStagePayload
            {
                Width = source.Width,
                Height = source.Height,
                Fps = source.Fps,
                TotalDuration = chunkVisuals.Sum(v => v.Duration),
                Style = source.Style,
                VisualTrack = chunkVisuals.Select(v => new VisualEvent
                {
                    SceneIndex = v.SceneIndex,
                    ImagePath = v.ImagePath,
                    StartTime = Math.Max(0, v.StartTime - chunkStart),
                    Duration = v.Duration,
                    EffectType = v.EffectType,
                    ZoomIntensity = v.ZoomIntensity,
                    TransitionType = v.TransitionType,
                    TransitionDuration = v.TransitionDuration,
                    VisualRole = v.VisualRole,
                    SegmentIndex = v.SegmentIndex,
                    SegmentCount = v.SegmentCount
                }).ToList(),
                AudioTrack = source.AudioTrack
                    .Where(a => a.Type == "voice" && a.StartTime >= chunkStart - 0.01 && a.StartTime < chunkEnd + 0.01)
                    .OrderBy(a => a.StartTime)
                    .Select(a => new AudioEvent
                    {
                        Type = a.Type,
                        FilePath = a.FilePath,
                        StartTime = Math.Max(0, a.StartTime - chunkStart),
                        Volume = a.Volume,
                        Loop = a.Loop
                    })
                    .ToList(),
                CaptionTrack = source.CaptionTrack
                    .Where(c => c.End > chunkStart && c.Start < chunkEnd)
                    .Select(c => new CaptionEvent
                    {
                        Text = c.Text,
                        Start = Math.Max(0, c.Start - chunkStart),
                        End = Math.Max(0.05, c.End - chunkStart)
                    })
                    .ToList()
            };
        }

        private async Task ConcatenateChunksAsync(
            List<string> chunkFiles,
            string outputPath,
            string chunkDir,
            CancellationToken ct,
            Func<string, Task>? logAsync)
        {
            var listPath = Path.Combine(chunkDir, "chunks.txt");
            var sb = new StringBuilder();

            foreach (var file in chunkFiles)
            {
                sb.AppendLine($"file '{ToConcatPath(file)}'");
            }

            await File.WriteAllTextAsync(listPath, sb.ToString(), new UTF8Encoding(false), ct);

            var args = $"-f concat -safe 0 -i \"{listPath}\" -c copy -y \"{outputPath}\"";

            try
            {
                await SafeLogAsync(logAsync, $"Render parçaları hızlı modda birleştiriliyor. Parça sayısı: {chunkFiles.Count}.");
                await RunFFmpegProcessAsync(args, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("Render parçaları hızlı modda birleştirildi."));
            }
            catch (Exception ex)
            {
                await SafeLogAsync(logAsync, PipelineLiveLog.Warning($"Hızlı parça birleştirme başarısız oldu. CPU ile yeniden kodlayarak birleştirilecek. Hata: {PipelineLiveLog.Shorten(ex.Message, 240)}"));
                var fallbackArgs = $"-f concat -safe 0 -i \"{listPath}\" -c:v libx264 -preset fast -pix_fmt yuv420p -c:a aac -b:a 192k -y \"{outputPath}\"";
                await RunFFmpegProcessAsync(fallbackArgs, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("Render parçaları CPU encode ile birleştirildi."));
            }
        }

        private async Task RunFFmpegWithFallbackAsync(
            SceneLayoutStagePayload layout,
            string subPath,
            string audioListPath,
            string? musicPath,
            string outputPath,
            string fontsDir,
            CancellationToken ct,
            Func<string, Task>? logAsync)
        {
            var args = BuildFFmpegCommand(layout, subPath, audioListPath, musicPath, outputPath, fontsDir, preferHardwareEncoder: true);

            try
            {
                await SafeLogAsync(logAsync, "FFmpeg GPU encode deneniyor. Encoder: h264_nvenc.");
                await RunFFmpegProcessAsync(args, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("FFmpeg GPU encode başarıyla tamamlandı. Encoder: h264_nvenc."));
            }
            catch (Exception ex) when (IsHardwareEncoderFailure(ex))
            {
                await SafeLogAsync(logAsync, PipelineLiveLog.Warning($"GPU encode kullanılamadı. CPU fallback'e geçiliyor. Encoder: libx264. Hata: {PipelineLiveLog.Shorten(ex.Message, 260)}"));
                var cpuArgs = BuildFFmpegCommand(layout, subPath, audioListPath, musicPath, outputPath, fontsDir, preferHardwareEncoder: false);
                await RunFFmpegProcessAsync(cpuArgs, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("FFmpeg CPU encode başarıyla tamamlandı. Encoder: libx264."));
            }
        }

        private static void NormalizeLayoutMediaPaths(SceneLayoutStagePayload layout, string workDir)
        {
            foreach (var visual in layout.VisualTrack)
            {
                visual.ImagePath = ResolveRunMediaPath(visual.ImagePath, workDir, "images");
            }

            foreach (var audio in layout.AudioTrack)
            {
                audio.FilePath = ResolveRunMediaPath(audio.FilePath, workDir, "audio");
            }
        }

        private static string ResolveRunMediaPath(string sourcePath, string workDir, string preferredSubFolder)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new FileNotFoundException($"Render media path is empty for '{preferredSubFolder}'.");

            if (File.Exists(sourcePath)) return sourcePath;

            var runRoot = Directory.GetParent(workDir)?.FullName;
            var fileName = GetPortableFileName(sourcePath);
            var tried = new List<string> { sourcePath };

            if (!string.IsNullOrWhiteSpace(runRoot) && !string.IsNullOrWhiteSpace(fileName))
            {
                var preferredCandidate = Path.Combine(runRoot, preferredSubFolder, fileName);
                tried.Add(preferredCandidate);
                if (File.Exists(preferredCandidate)) return preferredCandidate;

                var fallbackCandidate = Directory
                    .EnumerateFiles(runRoot, fileName, SearchOption.AllDirectories)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(fallbackCandidate))
                    return fallbackCandidate;
            }

            throw new FileNotFoundException(
                $"Render media file not found. Original='{sourcePath}'. Tried='{string.Join(" | ", tried)}'.");
        }

        private static string GetPortableFileName(string path)
        {
            var normalized = path
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            return Path.GetFileName(normalized);
        }

        // =================================================================
        // 🕵️‍♂️ AKILLI FONT BULUCU (Diskteki dosya adı ne olursa olsun bulur)
        // =================================================================
        private string GetSmartFontPath(string fontName)
        {
            string fontsFolder = Path.Combine(_assetsPath, "fonts");
            if (!Directory.Exists(fontsFolder)) return "Arial";

            // 1. Basit Arama: İsim birebir tutuyor mu?
            string directPath = Path.Combine(fontsFolder, fontName);
            if (File.Exists(directPath)) return directPath;
            if (File.Exists(directPath + ".ttf")) return directPath + ".ttf";
            if (File.Exists(directPath + ".otf")) return directPath + ".otf";

            // 2. Derin Arama: İsimden Aile Adını al (Poppins-Bold -> Poppins) ve klasörü tara
            var info = ParseFontInfo(fontName);
            string? foundPath = ScanFolderForFontFamily(fontsFolder, info.FamilyName);

            if (foundPath != null) return foundPath;

            // 3. Hiçbiri yoksa varsayılan (Arial vb.) döner
            return Directory.GetFiles(fontsFolder, "*.ttf").FirstOrDefault() ?? "Arial";
        }

        private string? ScanFolderForFontFamily(string folder, string targetFamily)
        {
            if (!Directory.Exists(folder)) return null;

            var allFonts = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".ttf") || s.EndsWith(".otf"));

            foreach (var file in allFonts)
            {
                // Her dosyanın binary başlığını oku
                string? internalFamily = GetFontFamilyName(file);

                // Eşleşme var mı?
                if (string.Equals(internalFamily, targetFamily, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
            return null;
        }

        // --- FONT PARSE HELPER ---
        private class FontInfo { public string FamilyName { get; set; } = "Arial"; public bool IsBold { get; set; } public bool IsItalic { get; set; } }

        private FontInfo ParseFontInfo(string rawName)
        {
            var info = new FontInfo();
            if (string.IsNullOrEmpty(rawName)) return info;

            // "Poppins-Bold" -> ["Poppins", "Bold"]
            var parts = rawName.Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // İlk parça Aile Adıdır
            info.FamilyName = parts[0];

            // Diğer parçalar özellik
            foreach (var part in parts)
            {
                if (part.Equals("Bold", StringComparison.OrdinalIgnoreCase)) info.IsBold = true;
                if (part.Equals("Italic", StringComparison.OrdinalIgnoreCase)) info.IsItalic = true;
            }
            return info;
        }

        // =================================================================
        // 🧙‍♂️ BINARY READER: TTF/OTF İÇİNDEN GERÇEK ADI OKUMA
        // =================================================================
        private string? GetFontFamilyName(string fontPath)
        {
            try
            {
                using var fs = File.OpenRead(fontPath);
                using var reader = new BinaryReader(fs);

                fs.Position = 4;
                ushort tableCount = Swap(reader.ReadUInt16());
                fs.Position = 12;

                long nameTableOffset = 0;
                for (int i = 0; i < tableCount; i++)
                {
                    uint tag = reader.ReadUInt32();
                    reader.ReadUInt32(); // Checksum
                    uint offset = Swap(reader.ReadUInt32());
                    reader.ReadUInt32(); // Length

                    if (tag == 0x656D616E) // 'name'
                    {
                        nameTableOffset = offset;
                        break;
                    }
                }

                if (nameTableOffset == 0) return null;

                fs.Position = nameTableOffset;
                ushort format = Swap(reader.ReadUInt16());
                ushort count = Swap(reader.ReadUInt16());
                ushort stringOffset = Swap(reader.ReadUInt16());
                long storageOffset = nameTableOffset + stringOffset;

                for (int i = 0; i < count; i++)
                {
                    ushort platformId = Swap(reader.ReadUInt16());
                    reader.ReadUInt16(); // encodingId
                    reader.ReadUInt16(); // languageId
                    ushort nameId = Swap(reader.ReadUInt16());
                    ushort length = Swap(reader.ReadUInt16());
                    ushort offset = Swap(reader.ReadUInt16());

                    // NameID 1 = Font Family Name, PlatformID 3 = Windows
                    if (nameId == 1 && (platformId == 3 || platformId == 0))
                    {
                        fs.Position = storageOffset + offset;
                        byte[] data = reader.ReadBytes(length);
                        return Encoding.BigEndianUnicode.GetString(data).Trim('\0');
                    }
                }
            }
            catch { return null; }
            return null;
        }

        private ushort Swap(ushort x) => (ushort)((x >> 8) | (x << 8));
        private uint Swap(uint x) => ((x >> 24) & 0xff) | ((x >> 8) & 0xff00) | ((x << 8) & 0xff0000) | ((x << 24) & 0xff000000);


        // =================================================================
        // 🛠️ FFmpeg KOMUT İNŞASI
        // =================================================================
        private string BuildFFmpegCommand(SceneLayoutStagePayload layout, string subPath, string audioListPath, string? musicPath, string outputPath, string fontsDir, bool preferHardwareEncoder)
        {
            var sb = new StringBuilder();
            var filter = new StringBuilder();

            string Escape(string p)
            {
                if (string.IsNullOrEmpty(p)) return "";
                // 🔥 Hem slashları düzelt, hem de iki noktayı (: -> \:) koru!
                return p.Replace("\\", "/").Replace(":", "\\:");
            }

            string FloatStr(double val) => val.ToString("0.00", CultureInfo.InvariantCulture);

            // GİRDİLER
            sb.Append($"-f concat -safe 0 -i \"{audioListPath}\" ");
            if (musicPath != null) sb.Append($"-stream_loop -1 -i \"{musicPath}\" ");

            int imgStartIndex = musicPath != null ? 2 : 1;
            foreach (var visual in layout.VisualTrack)
            {
                sb.Append($"-i \"{visual.ImagePath}\" ");
            }

            sb.Append("-filter_complex \"");

            // A) GÖRSEL EFEKTLER
            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                var v = layout.VisualTrack[i];
                int idx = imgStartIndex + i;
                int frames = Math.Max(1, (int)Math.Ceiling(v.Duration * layout.Fps));

                double maxZoom = layout.Style.VisualEffectsSettings.ZoomIntensity;
                if (maxZoom < 1.0) maxZoom = 1.1;

                var effectType = (v.EffectType ?? "zoom_in").Trim().ToLowerInvariant();
                var motion = BuildMotionExpressions(effectType, maxZoom, frames);

                filter.Append($"[{idx}:v]scale=-2:4*ih,zoompan=z='{motion.Zoom}':x='{motion.X}':y='{motion.Y}':d={frames}:s={layout.Width}x{layout.Height}:fps={layout.Fps},setsar=1");

                var cf = layout.Style.VisualEffectsSettings.ColorFilter;
                if (!string.IsNullOrEmpty(cf))
                {
                    if (cf == "bw_noir") filter.Append(",hue=s=0");
                    else if (cf == "cinematic_warm") filter.Append(",curves=r='0/0 1/1':g='0/0 0.8/1':b='0/0 0.8/1'");
                }
                filter.Append($"[v{i}];");
            }

            // B) CONCAT
            for (int i = 0; i < layout.VisualTrack.Count; i++) filter.Append($"[v{i}]");
            filter.Append($"concat=n={layout.VisualTrack.Count}:v=1:a=0[v_joined];");

            // C) BRANDING
            string lastVideoLabel = "[v_joined]";
            var branding = layout.Style.BrandingSettings;
            if (branding != null && branding.EnableWatermark)
            {
                string overlayPos = branding.Position switch
                {
                    "TopLeft" => "x=20:y=20",
                    "TopRight" => "x=W-w-20:y=20",
                    "BottomLeft" => "x=20:y=H-h-20",
                    _ => "x=W-w-20:y=H-h-20"
                };

                // Watermark fontu için basit bir fallback (Arial)
                var fontFolder = Path.Combine(_assetsPath, "fonts");
                string fontPath = Path.Combine(fontFolder, "Arial-Bold.ttf");
                if (!File.Exists(fontPath))
                {
                    fontPath = Directory.Exists(fontFolder)
                        ? Directory.GetFiles(fontFolder, "*.ttf").FirstOrDefault() ?? "Arial"
                        : "Arial";
                }

                string colorAss = HexToAssColor(branding.WatermarkColor, branding.Opacity);
                filter.Append($"[v_joined]drawtext=fontfile='{Escape(fontPath)}':text='{branding.WatermarkText}':fontcolor={colorAss}:fontsize=24:{overlayPos}[v_branded];");
                lastVideoLabel = "[v_branded]";
            }

            // D) ALTYAZILAR
            var capSettings = layout.Style.CaptionSettings;
            if (capSettings.EnableCaptions)
            {
                string assFile = Escape(subPath);
                string escapedFontsDir = Escape(fontsDir);
                filter.Append($"{lastVideoLabel}subtitles='{assFile}':fontsdir='{escapedFontsDir}'[v_final];");
            }
            else
            {
                filter.Append($"{lastVideoLabel}null[v_final];");
            }

            // E) SES MİKSAJI
            var mix = layout.Style.AudioMixSettings;
            if (musicPath != null)
            {
                double musicVol = mix.MusicVolumePercent / 100.0;
                double voiceVol = mix.VoiceVolumePercent / 100.0;
                filter.Append($"[1:a]volume={FloatStr(musicVol)}[bg];");
                filter.Append($"[0:a]volume={FloatStr(voiceVol)}[fg];");

                if (mix.EnableDucking)
                {
                    filter.Append($"[bg][fg]sidechaincompress=threshold=0.05:ratio=5:attack=50:release=300[bg_ducked];");
                    filter.Append($"[fg][bg_ducked]amix=inputs=2:duration=first[a_mixed];");
                }
                else
                {
                    filter.Append($"[fg][bg]amix=inputs=2:duration=first[a_mixed];");
                }

                if (mix.FadeAudioInOut)
                {
                    string d = FloatStr(mix.FadeDurationSec);
                    string fadeOutStart = FloatStr(Math.Max(0, layout.TotalDuration - mix.FadeDurationSec));
                    filter.Append($"[a_mixed]afade=t=in:st=0:d={d},afade=t=out:st={fadeOutStart}:d={d}[a_final]");
                }
                else
                {
                    filter.Append($"[a_mixed]anull[a_final]");
                }
            }
            else
            {
                filter.Append($"[0:a]volume={FloatStr(mix.VoiceVolumePercent / 100.0)}[a_final]");
            }

            filter.Append("\"");

            // ÇIKTI
            sb.Append(filter.ToString());
            sb.Append(" -map \"[v_final]\" -map \"[a_final]\" ");
            if (preferHardwareEncoder)
            {
                string gpuPreset = layout.Style.EncoderPreset switch { "fast" => "p3", "slow" => "p6", _ => "p4" };
                sb.Append($"-c:v h264_nvenc -preset {gpuPreset} ");
            }
            else
            {
                string cpuPreset = layout.Style.EncoderPreset switch
                {
                    "ultrafast" => "ultrafast",
                    "fast" => "fast",
                    "slow" => "slow",
                    _ => "medium"
                };
                sb.Append($"-c:v libx264 -preset {cpuPreset} ");
            }
            sb.Append($"-b:v {layout.Style.BitrateKbps}k -maxrate {layout.Style.BitrateKbps + 1000}k -bufsize 10M ");
            sb.Append("-pix_fmt yuv420p ");
            sb.Append("-c:a aac -b:a 192k ");
            sb.Append("-shortest ");
            sb.Append($"-y \"{outputPath}\"");

            return sb.ToString();
        }

        private static (string Zoom, string X, string Y) BuildMotionExpressions(string effectType, double maxZoom, int frames)
        {
            string FloatStr(double val) => val.ToString("0.00", CultureInfo.InvariantCulture);

            var progressDenominator = Math.Max(1, frames - 1);
            var centerX = "iw/2-(iw/zoom/2)";
            var centerY = "ih/2-(ih/zoom/2)";
            var panZoom = Math.Max(maxZoom, 1.08);
            var panZoomExpr = FloatStr(panZoom);

            return effectType switch
            {
                "zoom_out" => (
                    $"if(eq(on,1),{FloatStr(maxZoom)},max(1.0,zoom-0.0015))",
                    centerX,
                    centerY),
                "pan_left" => (
                    panZoomExpr,
                    $"(iw-iw/zoom)*on/{progressDenominator}",
                    centerY),
                "pan_right" => (
                    panZoomExpr,
                    $"(iw-iw/zoom)*(1-on/{progressDenominator})",
                    centerY),
                "pan_up" => (
                    panZoomExpr,
                    centerX,
                    $"(ih-ih/zoom)*on/{progressDenominator}"),
                "pan_down" => (
                    panZoomExpr,
                    centerX,
                    $"(ih-ih/zoom)*(1-on/{progressDenominator})"),
                _ => (
                    $"min(zoom+0.0015,{FloatStr(maxZoom)})",
                    centerX,
                    centerY)
            };
        }


        // =================================================================
        // 🔥 ASS GENERATOR (WORD MERGER + BLACK-ON-BLACK FIX)
        // =================================================================
        private async Task GenerateDynamicAssFileAsync(List<CaptionEvent> captions, string path, RenderCaptionSettings settings, string cultureCode, FontInfo fontInfo, int videoWidth, int videoHeight)
        {
            var sb = new StringBuilder();
            CultureInfo culture;
            try { culture = new CultureInfo(cultureCode); } catch { culture = new CultureInfo("en-US"); }

            // 1. FONT VE OKUNABİLİRLİK AYARLARI
            videoWidth = videoWidth > 0 ? videoWidth : 1080;
            videoHeight = videoHeight > 0 ? videoHeight : 1920;

            int minReadableFontSize = Math.Max(18, (int)Math.Round(Math.Min(videoWidth, videoHeight) * 0.035));
            int finalFontSize = settings.FontSize > 0 ? settings.FontSize : minReadableFontSize;
            if (finalFontSize < minReadableFontSize) finalFontSize = minReadableFontSize;

            // ASS Formatında Bold/Italic (-1 = True)
            int bold = fontInfo.IsBold ? -1 : 0;
            int italic = fontInfo.IsItalic ? -1 : 0;
            string familyName = fontInfo.FamilyName;

            sb.AppendLine("[Script Info]");
            sb.AppendLine("ScriptType: v4.00+");
            sb.AppendLine("WrapStyle: 2");
            sb.AppendLine($"PlayResX: {videoWidth}");
            sb.AppendLine($"PlayResY: {videoHeight}");
            sb.AppendLine();

            sb.AppendLine("[V4+ Styles]");
            sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");

            // RENKLER
            string primary = HexToAssColor(settings.PrimaryColor);
            string highlightBoxColor = HexToAssColor(settings.HighlightColor ?? "#000000", 1.0);
            string normalOutlineColor = HexToAssColor(settings.OutlineColor);

            string backColor = normalOutlineColor;
            int borderStyle = 1;
            int outlineSize = settings.OutlineSize;
            double spacing = 1.0;

            if (settings.EnableHighlight)
            {
                // Kutu Rengi = Siyah (Vurgu)
                backColor = highlightBoxColor;
                // İnceltilmiş Outline
                outlineSize = (int)(finalFontSize * 0.15);
                if (outlineSize < 5) outlineSize = 5;
                // Harf aralığı
                spacing = 3.0;
            }

            int align = settings.Position switch
            {
                CaptionPositionTypes.Bottom => 2,
                CaptionPositionTypes.Center => 5,
                CaptionPositionTypes.Top => 8,
                _ => 2
            };

            int marginV = settings.MarginBottom;
            int minBottomMargin = Math.Max(40, (int)Math.Round(videoHeight * 0.10));
            if (align == 2 && marginV < minBottomMargin) marginV = minBottomMargin;

            // Style: FamilyName dinamik
            sb.AppendLine($"Style: Default,{familyName},{finalFontSize},{primary},&H000000FF,{backColor},&H00000000,{bold},{italic},0,0,100,100,{spacing},0,{borderStyle},{outlineSize},0,{align},20,20,{marginV},1");
            sb.AppendLine();

            sb.AppendLine("[Events]");
            sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            // --- KELİME BİRLEŞTİRME (MERGER) ---
            int targetWordCount = settings.MaxWordsPerLine > 0 ? settings.MaxWordsPerLine : 2;

            // 1. Tüm kelimeleri düzleştir
            var flatWordList = new List<(string Word, double Start, double End)>();

            foreach (var cap in captions)
            {
                string txt = settings.Uppercase ? cap.Text.ToUpper(culture) : cap.Text;
                var words = txt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                double totalDur = cap.End - cap.Start;
                double perWord = totalDur / words.Length;

                for (int i = 0; i < words.Length; i++)
                {
                    double wStart = cap.Start + (i * perWord);
                    double wEnd = wStart + perWord;
                    flatWordList.Add((words[i], wStart, wEnd));
                }
            }

            // 2. Grupla ve Yaz
            for (int i = 0; i < flatWordList.Count; i += targetWordCount)
            {
                var chunk = flatWordList.Skip(i).Take(targetWordCount).ToList();
                if (!chunk.Any()) break;

                string lineText = string.Join(" ", chunk.Select(c => c.Word));
                double startTimeMs = chunk.First().Start * 1000;
                double endTimeMs = chunk.Last().End * 1000;

                // Animasyon: Sadece Pop Up (Renk değişimi yok)
                string animTags = settings.EnableHighlight ? "{\\t(0,100,\\fscx110\\fscy110)}" : "";

                TimeSpan start = TimeSpan.FromMilliseconds(startTimeMs);
                TimeSpan end = TimeSpan.FromMilliseconds(endTimeMs);

                sb.AppendLine($"Dialogue: 0,{FormatAssTime(start)},{FormatAssTime(end)},Default,,0,0,0,,{animTags}{lineText}");
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        // --- HELPER METODLAR ---
        private string HexToAssColor(string hex, double opacity = 1.0)
        {
            if (string.IsNullOrEmpty(hex)) return "&H00FFFFFF";
            hex = hex.Replace("#", "");
            string r = "FF", g = "FF", b = "FF";
            if (hex.Length >= 6) { r = hex.Substring(0, 2); g = hex.Substring(2, 2); b = hex.Substring(4, 2); }
            int alphaInt = (int)((1.0 - opacity) * 255);
            string alphaHex = alphaInt.ToString("X2");
            return $"&H{alphaHex}{b}{g}{r}";
        }

        private string FormatAssTime(TimeSpan t) => $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds / 10:D2}";

        private string? GetRandomMusicFile()
        {
            var musicDir = Path.Combine(_assetsPath, "music");
            if (!Directory.Exists(musicDir)) return null;
            var files = Directory.GetFiles(musicDir, "*.mp3");
            return files.Length > 0 ? files[new Random().Next(files.Length)] : null;
        }

        private async Task GenerateAudioListAsync(List<AudioEvent> audios, string path)
        {
            var sb = new StringBuilder();
            foreach (var audio in audios.Where(a => a.Type == "voice").OrderBy(a => a.StartTime))
            {
                var filePath = ToConcatPath(audio.FilePath);
                sb.AppendLine($"file '{filePath}'");
            }
            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static string ToConcatPath(string path)
            => path.Replace("\\", "/").Replace("'", "\\'");

        private async Task RunFFmpegProcessAsync(string args, CancellationToken ct)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            var errLog = new StringBuilder();
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) errLog.AppendLine(e.Data); };
            p.Start();
            p.BeginErrorReadLine();
            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0)
            {
                var logStr = errLog.ToString();
                var errPart = logStr.Length > 1000 ? logStr.Substring(logStr.Length - 1000) : logStr;
                throw new Exception($"FFmpeg Hatası: {errPart}");
            }
        }

        private static async Task SafeLogAsync(Func<string, Task>? logAsync, string message)
        {
            if (logAsync == null) return;

            try
            {
                await logAsync(message);
            }
            catch
            {
                // Render akışı log gönderimi yüzünden bozulmasın.
            }
        }

        private static bool IsHardwareEncoderFailure(Exception ex)
        {
            var message = ex.ToString();
            return message.Contains("h264_nvenc", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("Cannot load", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("No capable devices", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("Error while opening encoder", StringComparison.OrdinalIgnoreCase);
        }
    }
}
