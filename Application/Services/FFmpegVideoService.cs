using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Entity;
using Core.Entity.Models;
using Core.Enums;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class FFmpegVideoService : IVideoRendererService
    {
        private const int MaxVisualsPerRenderChunk = 24;
        private const int MaxCommandLengthBeforeChunking = 22000;
        private static readonly Regex FfmpegTimeRegex = new(@"time=(?<hours>\d{2,}):(?<minutes>\d{2}):(?<seconds>\d{2}(?:\.\d+)?)", RegexOptions.Compiled);

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
            Func<string, Task>? logAsync = null,
            Func<RenderProgressUpdate, Task>? progressAsync = null)
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
                    await RenderChunkedAsync(layout, workDir, tempId, musicPath, outputPath, fontsDir, cultureCode, fontInfo, ct, logAsync, progressAsync);
                }
                else
                {
                    await SafeLogAsync(logAsync, "FFmpeg tek geçiş render modu kullanılacak.");
                    await RenderSinglePassAsync(layout, srtPath, audioListPath, musicPath, outputPath, fontsDir, cultureCode, fontInfo, ct, logAsync, progressAsync);
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
            Func<string, Task>? logAsync,
            Func<RenderProgressUpdate, Task>? progressAsync,
            double progressStartPercent = 0,
            double progressEndPercent = 100,
            double? progressTotalSeconds = null,
            int? chunkIndex = null,
            int? totalChunks = null,
            bool forceVoiceConcat = false)
        {
            await GenerateDynamicAssFileAsync(layout.CaptionTrack, subPath, layout.Style.CaptionSettings, cultureCode, fontInfo, layout.Width, layout.Height);
            await GenerateAudioListAsync(layout.AudioTrack, audioListPath);
            await LogRenderCompilerSummaryAsync(layout, logAsync);
            if (forceVoiceConcat)
            {
                await SafeLogAsync(logAsync, "Parçalı render ses modu: voice concat kullanılacak. Chunk içinde tüm sahne sesleri sırayla korunacak.");
            }
            await RunFFmpegWithFallbackAsync(
                layout,
                subPath,
                audioListPath,
                musicPath,
                outputPath,
                fontsDir,
                ct,
                logAsync,
                progressAsync,
                progressStartPercent,
                progressEndPercent,
                progressTotalSeconds,
                chunkIndex,
                totalChunks,
                forceVoiceConcat);
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
            Func<string, Task>? logAsync,
            Func<RenderProgressUpdate, Task>? progressAsync)
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
                await SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
                {
                    Label = "Parcali render basladi",
                    Percent = 1,
                    CurrentSeconds = layout.TotalDuration * 0.01,
                    TotalSeconds = layout.TotalDuration,
                    TotalChunks = totalChunks
                });

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
                    var progressStart = ((chunkIndex - 1) / (double)totalChunks) * 92;
                    var progressEnd = (chunkIndex / (double)totalChunks) * 92;
                    await RenderSinglePassAsync(
                        chunkLayout,
                        chunkSubPath,
                        chunkAudioListPath,
                        musicPath,
                        chunkOutputPath,
                        fontsDir,
                        cultureCode,
                        fontInfo,
                        ct,
                        logAsync,
                        progressAsync,
                        progressStart,
                        progressEnd,
                        progressTotalSeconds: layout.TotalDuration,
                        chunkIndex: chunkIndex,
                        totalChunks: totalChunks,
                        forceVoiceConcat: true);
                    chunkFiles.Add(chunkOutputPath);
                }

                if (chunkFiles.Count == 0)
                    throw new InvalidOperationException("Chunk render icin sahne bulunamadi.");

                if (chunkFiles.Count == 1)
                {
                    await SafeLogAsync(logAsync, PipelineLiveLog.Success("Tek render parçası üretildi. Final dosyaya kopyalanıyor."));
                    File.Copy(chunkFiles[0], outputPath, overwrite: true);
                    await SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
                    {
                        Label = "Render tamamlandi",
                        Percent = 100,
                        CurrentSeconds = layout.TotalDuration,
                        TotalSeconds = layout.TotalDuration,
                        ChunkIndex = 1,
                        TotalChunks = 1,
                        IsCompleted = true
                    });
                    return;
                }

                await ConcatenateChunksAsync(chunkFiles, outputPath, chunkDir, layout.TotalDuration, ct, logAsync, progressAsync);
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
                    OverlayText = v.OverlayText,
                    Emphasis = v.Emphasis,
                    VisualRole = v.VisualRole,
                    VisualType = v.VisualType,
                    VarietyRole = v.VarietyRole,
                    VarietyReason = v.VarietyReason,
                    SegmentRole = v.SegmentRole,
                    SegmentIndex = v.SegmentIndex,
                    SegmentCount = v.SegmentCount,
                    ShotType = v.ShotType,
                    DirectorIntent = v.DirectorIntent,
                    CutReason = v.CutReason,
                    AudioTransition = v.AudioTransition,
                    AudioOffsetSec = v.AudioOffsetSec,
                    ChapterTitle = v.ChapterTitle,
                    CaptionMode = v.CaptionMode,
                    MusicEnergy = v.MusicEnergy,
                    ContinuityAnchor = v.ContinuityAnchor,
                    Composition = v.Composition,
                    VisualQualityScore = v.VisualQualityScore,
                    VisualQualityNotes = v.VisualQualityNotes
                }).ToList(),
                AudioTrack = source.AudioTrack
                    .Where(a => AudioEventOverlapsChunk(a, chunkStart, chunkEnd))
                    .OrderBy(a => a.StartTime)
                    .Select(a =>
                    {
                        var sourceTrim = Math.Max(0, chunkStart - a.StartTime);
                        return new AudioEvent
                        {
                            Type = a.Type,
                            FilePath = a.FilePath,
                            SceneNumber = a.SceneNumber,
                            StartTime = Math.Max(0, a.StartTime - chunkStart),
                            Duration = Math.Max(0.05, (a.Duration > 0 ? a.Duration : GetFallbackAudioEventDuration(a)) - sourceTrim),
                            SourceStartOffsetSec = a.SourceStartOffsetSec + sourceTrim,
                            Volume = a.Volume,
                            FadeInSec = sourceTrim > 0 ? 0 : a.FadeInSec,
                            FadeOutSec = a.FadeOutSec,
                            EditTransition = a.EditTransition,
                            EditOffsetSec = a.EditOffsetSec,
                            Loop = a.Loop
                        };
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
            double expectedDuration,
            CancellationToken ct,
            Func<string, Task>? logAsync,
            Func<RenderProgressUpdate, Task>? progressAsync)
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
                await SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
                {
                    Label = "Render parcalari birlestiriliyor",
                    Percent = 94,
                    CurrentSeconds = expectedDuration * 0.94,
                    TotalSeconds = expectedDuration
                });
                await RunFFmpegProcessAsync(args, ct);
                await EnsureOutputDurationAsync(outputPath, expectedDuration, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("Render parçaları hızlı modda birleştirildi."));
            }
            catch (Exception ex)
            {
                await SafeLogAsync(logAsync, PipelineLiveLog.Warning($"Hızlı parça birleştirme başarısız oldu. CPU ile yeniden kodlayarak birleştirilecek. Hata: {PipelineLiveLog.Shorten(ex.Message, 240)}"));
                var fallbackArgs = $"-f concat -safe 0 -i \"{listPath}\" -c:v libx264 -preset fast -pix_fmt yuv420p -c:a aac -b:a 192k -y \"{outputPath}\"";
                await SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
                {
                    Label = "Render parcalari CPU ile birlestiriliyor",
                    Percent = 96,
                    CurrentSeconds = expectedDuration * 0.96,
                    TotalSeconds = expectedDuration
                });
                await RunFFmpegProcessAsync(fallbackArgs, ct);
                await EnsureOutputDurationAsync(outputPath, expectedDuration, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("Render parçaları CPU encode ile birleştirildi."));
            }

            await SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
            {
                Label = "Render tamamlandi",
                Percent = 100,
                CurrentSeconds = expectedDuration,
                TotalSeconds = expectedDuration,
                IsCompleted = true
            });
        }

        private async Task RunFFmpegWithFallbackAsync(
            SceneLayoutStagePayload layout,
            string subPath,
            string audioListPath,
            string? musicPath,
            string outputPath,
            string fontsDir,
            CancellationToken ct,
            Func<string, Task>? logAsync,
            Func<RenderProgressUpdate, Task>? progressAsync,
            double progressStartPercent = 0,
            double progressEndPercent = 100,
            double? progressTotalSeconds = null,
            int? chunkIndex = null,
            int? totalChunks = null,
            bool forceVoiceConcat = false)
        {
            var args = BuildFFmpegCommand(layout, subPath, audioListPath, musicPath, outputPath, fontsDir, preferHardwareEncoder: true, forceVoiceConcat);
            var chunkLabelSuffix = totalChunks.HasValue && chunkIndex.HasValue
                ? $" ({chunkIndex}/{totalChunks})"
                : "";
            var totalRenderSeconds = progressTotalSeconds.GetValueOrDefault(layout.TotalDuration);

            try
            {
                await SafeLogAsync(logAsync, "FFmpeg GPU encode deneniyor. Encoder: h264_nvenc.");
                await RunFFmpegProcessAsync(
                    args,
                    ct,
                    layout.TotalDuration,
                    CreateRenderProgressReporter(
                        progressAsync,
                        layout.TotalDuration,
                        totalRenderSeconds,
                        progressStartPercent,
                        progressEndPercent,
                        $"GPU render{chunkLabelSuffix}",
                        chunkIndex,
                        totalChunks));
                await EnsureOutputDurationAsync(outputPath, layout.TotalDuration, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("FFmpeg GPU encode başarıyla tamamlandı. Encoder: h264_nvenc."));
            }
            catch (Exception ex) when (IsHardwareEncoderFailure(ex))
            {
                await SafeLogAsync(logAsync, PipelineLiveLog.Warning($"GPU encode kullanılamadı. CPU fallback'e geçiliyor. Encoder: libx264. Hata: {PipelineLiveLog.Shorten(ex.Message, 260)}"));
                var cpuArgs = BuildFFmpegCommand(layout, subPath, audioListPath, musicPath, outputPath, fontsDir, preferHardwareEncoder: false, forceVoiceConcat);
                await RunFFmpegProcessAsync(
                    cpuArgs,
                    ct,
                    layout.TotalDuration,
                    CreateRenderProgressReporter(
                        progressAsync,
                        layout.TotalDuration,
                        totalRenderSeconds,
                        progressStartPercent,
                        progressEndPercent,
                        $"CPU render{chunkLabelSuffix}",
                        chunkIndex,
                        totalChunks));
                await EnsureOutputDurationAsync(outputPath, layout.TotalDuration, ct);
                await SafeLogAsync(logAsync, PipelineLiveLog.Success("FFmpeg CPU encode başarıyla tamamlandı. Encoder: libx264."));
            }

            await SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
            {
                Label = $"Render parcasi tamamlandi{chunkLabelSuffix}",
                Percent = progressEndPercent,
                CurrentSeconds = totalRenderSeconds * (Math.Clamp(progressEndPercent, 0, 100) / 100),
                TotalSeconds = totalRenderSeconds,
                ChunkIndex = chunkIndex,
                TotalChunks = totalChunks,
                IsCompleted = progressEndPercent >= 100
            });
        }

        private static void NormalizeLayoutMediaPaths(SceneLayoutStagePayload layout, string workDir)
        {
            foreach (var visual in layout.VisualTrack)
            {
                visual.ImagePath = ResolveRunMediaPath(visual.ImagePath, workDir, "images");
            }

            foreach (var audio in layout.AudioTrack)
            {
                if (IsSyntheticSfx(audio.FilePath)) continue;
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
        private string BuildFFmpegCommand(SceneLayoutStagePayload layout, string subPath, string audioListPath, string? musicPath, string outputPath, string fontsDir, bool preferHardwareEncoder, bool forceVoiceConcat = false)
        {
            var sb = new StringBuilder();
            var filter = new StringBuilder();
            var orderedVisuals = layout.VisualTrack
                .OrderBy(v => v.StartTime)
                .ThenBy(v => v.SceneIndex)
                .ThenBy(v => v.SegmentIndex)
                .ToList();
            var sfxEvents = layout.AudioTrack
                .Where(a => string.Equals(a.Type, "sfx", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(a.FilePath)
                            && (File.Exists(a.FilePath) || IsSyntheticSfx(a.FilePath)))
                .OrderBy(a => a.StartTime)
                .ToList();
            var voiceEvents = layout.AudioTrack
                .Where(a => string.Equals(a.Type, "voice", StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(a.FilePath)
                            && File.Exists(a.FilePath))
                .OrderBy(a => a.StartTime)
                .ToList();
            var useVoiceTimeline = !forceVoiceConcat && ShouldUseVoiceTimeline(voiceEvents);

            string Escape(string p)
            {
                if (string.IsNullOrEmpty(p)) return "";
                // 🔥 Hem slashları düzelt, hem de iki noktayı (: -> \:) koru!
                return p.Replace("\\", "/").Replace(":", "\\:");
            }

            string FloatStr(double val) => val.ToString("0.00", CultureInfo.InvariantCulture);
            string Label(string value) => $"[{value}]";
            string DrawTextFontOption(string fontPathOrName)
                => File.Exists(fontPathOrName)
                    ? $"fontfile='{Escape(fontPathOrName)}'"
                    : $"font='{EscapeDrawText(string.IsNullOrWhiteSpace(fontPathOrName) ? "Arial" : fontPathOrName)}'";

            // GİRDİLER
            var nextInputIndex = 0;
            var voiceInputStartIndex = 0;
            if (useVoiceTimeline)
            {
                foreach (var voice in voiceEvents)
                {
                    sb.Append($"-i \"{voice.FilePath}\" ");
                }

                nextInputIndex += voiceEvents.Count;
            }
            else
            {
                sb.Append($"-f concat -safe 0 -i \"{audioListPath}\" ");
                nextInputIndex = 1;
            }

            var musicInputIndex = -1;
            if (musicPath != null)
            {
                musicInputIndex = nextInputIndex++;
                sb.Append($"-stream_loop -1 -i \"{musicPath}\" ");
            }

            var sfxInputStartIndex = nextInputIndex;
            foreach (var sfx in sfxEvents)
            {
                if (IsSyntheticSfx(sfx.FilePath))
                {
                    sb.Append($"-f lavfi -i \"{BuildSyntheticSfxSource(sfx.FilePath)}\" ");
                }
                else
                {
                    sb.Append($"-i \"{sfx.FilePath}\" ");
                }

                nextInputIndex++;
            }

            int imgStartIndex = nextInputIndex;
            foreach (var visual in orderedVisuals)
            {
                sb.Append($"-i \"{visual.ImagePath}\" ");
            }

            sb.Append("-filter_complex \"");

            // A) GÖRSEL EFEKTLER
            var enableOverlayText = layout.Style.VisualEffectsSettings.EnableOverlayText;
            for (int i = 0; i < orderedVisuals.Count; i++)
            {
                var v = orderedVisuals[i];
                int idx = imgStartIndex + i;
                var incomingTransitionDuration = i == 0
                    ? 0
                    : GetEffectiveTransitionDuration(v, orderedVisuals[i - 1]);
                var clipDuration = Math.Max(0.05, v.Duration + incomingTransitionDuration);
                int frames = Math.Max(1, (int)Math.Ceiling(clipDuration * layout.Fps));

                double maxZoom = layout.Style.VisualEffectsSettings.ZoomIntensity;
                if (maxZoom < 1.0) maxZoom = 1.1;

                var effectType = (v.EffectType ?? "zoom_in").Trim().ToLowerInvariant();
                var motion = BuildMotionExpressions(effectType, maxZoom, frames);

                filter.Append($"[{idx}:v]scale=-2:4*ih,zoompan=z='{EscapeFilterExpression(motion.Zoom)}':x='{EscapeFilterExpression(motion.X)}':y='{EscapeFilterExpression(motion.Y)}':d={frames}:s={layout.Width}x{layout.Height}:fps={layout.Fps},setsar=1");

                var cf = layout.Style.VisualEffectsSettings.ColorFilter;
                if (!string.IsNullOrEmpty(cf))
                {
                    if (cf == "bw_noir") filter.Append(",hue=s=0");
                    else if (cf == "cinematic_warm") filter.Append(",curves=r='0/0 1/1':g='0/0 0.8/1':b='0/0 0.8/1'");
                }

                if (enableOverlayText && !string.IsNullOrWhiteSpace(v.OverlayText))
                {
                    var overlayFont = Directory.Exists(fontsDir)
                        ? Directory.GetFiles(fontsDir, "*.ttf").FirstOrDefault()
                        : null;
                    overlayFont ??= GetSmartFontPath(layout.Style.CaptionSettings.FontName);
                    var overlayFontSize = Math.Max(28, (int)Math.Round(Math.Min(layout.Width, layout.Height) * 0.045));
                    var boxBorder = Math.Max(10, (int)Math.Round(overlayFontSize * 0.35));
                    var y = layout.Height >= layout.Width ? "h*0.16" : "h*0.12";

                    filter.Append($",drawtext={DrawTextFontOption(overlayFont)}:text='{EscapeDrawText(v.OverlayText)}':fontcolor=white:fontsize={overlayFontSize}:box=1:boxcolor=black@0.52:boxborderw={boxBorder}:x=(w-text_w)/2:y={y}");
                }

                filter.Append(",format=yuv420p,settb=AVTB,setpts=PTS-STARTPTS");
                filter.Append($"[v{i}];");
            }

            // B) EDITOR TRANSITION COMPILER
            string lastVideoLabel;
            if (orderedVisuals.Count == 0)
            {
                throw new InvalidOperationException("Render için visual track boş.");
            }
            else if (orderedVisuals.Count == 1)
            {
                lastVideoLabel = "[v0]";
            }
            else
            {
                var currentLabel = "v0";
                var currentDuration = Math.Max(0.05, orderedVisuals[0].Duration);

                for (var i = 1; i < orderedVisuals.Count; i++)
                {
                    var currentVisual = orderedVisuals[i];
                    var transitionDuration = GetEffectiveTransitionDuration(currentVisual, orderedVisuals[i - 1]);
                    var transitionName = NormalizeXfadeTransition(currentVisual.TransitionType);
                    var outputLabel = $"v_join_{i}";

                    if (transitionDuration <= 0 || string.IsNullOrWhiteSpace(transitionName))
                    {
                        filter.Append($"{Label(currentLabel)}[v{i}]concat=n=2:v=1:a=0,format=yuv420p,settb=AVTB,setpts=PTS-STARTPTS{Label(outputLabel)};");
                    }
                    else
                    {
                        var offset = Math.Max(0, currentDuration - transitionDuration);
                        filter.Append($"{Label(currentLabel)}[v{i}]xfade=transition={transitionName}:duration={FloatStr(transitionDuration)}:offset={FloatStr(offset)},format=yuv420p,settb=AVTB,setpts=PTS-STARTPTS{Label(outputLabel)};");
                    }

                    currentLabel = outputLabel;
                    currentDuration += Math.Max(0.05, currentVisual.Duration);
                }

                lastVideoLabel = Label(currentLabel);
            }

            // C) BRANDING
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
                filter.Append($"{lastVideoLabel}drawtext={DrawTextFontOption(fontPath)}:text='{EscapeDrawText(branding.WatermarkText)}':fontcolor={colorAss}:fontsize=24:{overlayPos}[v_branded];");
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
            if (useVoiceTimeline)
            {
                var voiceLabels = new List<string>();
                for (var i = 0; i < voiceEvents.Count; i++)
                {
                    var voice = voiceEvents[i];
                    var label = $"voice_{i}";
                    var inputIndex = voiceInputStartIndex + i;
                    var delayMs = Math.Max(0, (int)Math.Round(voice.StartTime * 1000));
                    var duration = voice.Duration > 0 ? voice.Duration : GetFallbackAudioEventDuration(voice);
                    var sourceStart = Math.Max(0, voice.SourceStartOffsetSec);
                    var volume = voice.Volume > 0
                        ? voice.Volume
                        : Math.Clamp(mix.VoiceVolumePercent / 100.0, 0.0, 2.0);
                    var fadeIn = Math.Clamp(voice.FadeInSec, 0.0, Math.Min(0.15, duration * 0.25));
                    var fadeOut = Math.Clamp(voice.FadeOutSec, 0.0, Math.Min(0.15, duration * 0.25));

                    filter.Append($"[{inputIndex}:a]atrim=start={FloatStr(sourceStart)}:duration={FloatStr(duration)},asetpts=PTS-STARTPTS,volume={FloatStr(volume)}");
                    if (fadeIn > 0)
                    {
                        filter.Append($",afade=t=in:st=0:d={FloatStr(fadeIn)}");
                    }
                    if (fadeOut > 0)
                    {
                        var fadeOutStart = Math.Max(0.01, duration - fadeOut);
                        filter.Append($",afade=t=out:st={FloatStr(fadeOutStart)}:d={FloatStr(fadeOut)}");
                    }
                    filter.Append($",adelay={delayMs}:all=1[{label}];");
                    voiceLabels.Add(label);
                }

                if (voiceLabels.Count == 1)
                {
                    filter.Append($"{Label(voiceLabels[0])}anull[voice_base];");
                }
                else
                {
                    foreach (var label in voiceLabels) filter.Append(Label(label));
                    filter.Append($"amix=inputs={voiceLabels.Count}:duration=longest:dropout_transition=0:normalize=0,atrim=duration={FloatStr(layout.TotalDuration + 0.5)}[voice_base];");
                }
            }
            else
            {
                filter.Append($"[0:a]volume={FloatStr(mix.VoiceVolumePercent / 100.0)}[voice_base];");
            }

            var voiceBaseLabel = "voice_base";
            if (mix.EnableVoiceLoudnessNormalization)
            {
                var targetI = Math.Clamp(mix.VoiceLoudnessTargetI, -24.0, -10.0);
                var targetTp = Math.Clamp(mix.VoiceLoudnessTargetTp, -6.0, -0.5);
                var targetLra = Math.Clamp(mix.VoiceLoudnessRange, 3.0, 20.0);
                filter.Append($"[voice_base]loudnorm=I={FloatStr(targetI)}:TP={FloatStr(targetTp)}:LRA={FloatStr(targetLra)}:linear=true:print_format=summary[voice_master];");
                voiceBaseLabel = "voice_master";
            }

            var baseAudioLabel = voiceBaseLabel;
            if (musicPath != null)
            {
                double musicVol = mix.MusicVolumePercent / 100.0;
                filter.Append($"[{musicInputIndex}:a]volume={FloatStr(musicVol)}[bg];");

                if (mix.EnableDucking)
                {
                    var duckThreshold = Math.Clamp(mix.DuckingThreshold, 0.001, 0.5);
                    var duckRatio = Math.Clamp(mix.DuckingRatio, 1.0, 20.0);
                    var duckAttack = Math.Clamp(mix.DuckingAttackMs, 5, 1000);
                    var duckRelease = Math.Clamp(mix.DuckingReleaseMs, 40, 3000);
                    filter.Append($"[bg][{voiceBaseLabel}]sidechaincompress=threshold={FloatStr(duckThreshold)}:ratio={FloatStr(duckRatio)}:attack={duckAttack}:release={duckRelease}[bg_ducked];");
                    filter.Append($"[{voiceBaseLabel}][bg_ducked]amix=inputs=2:duration=first:normalize=0[a_base];");
                }
                else
                {
                    filter.Append($"[{voiceBaseLabel}][bg]amix=inputs=2:duration=first:normalize=0[a_base];");
                }

                baseAudioLabel = "a_base";
            }

            var sfxLabels = new List<string>();
            for (var i = 0; i < sfxEvents.Count; i++)
            {
                var sfx = sfxEvents[i];
                var delayMs = Math.Max(0, (int)Math.Round(sfx.StartTime * 1000));
                var volume = sfx.Volume > 0
                    ? sfx.Volume
                    : Math.Clamp(mix.SfxVolumePercent / 100.0, 0.0, 2.0);
                var label = $"sfx_{i}";
                var inputIndex = sfxInputStartIndex + i;
                var fadeOutStart = Math.Max(0.02, GetSfxCueDuration(sfx.FilePath) - 0.06);

                filter.Append($"[{inputIndex}:a]volume={FloatStr(volume)},afade=t=in:st=0:d=0.01,afade=t=out:st={FloatStr(fadeOutStart)}:d=0.06,adelay={delayMs}:all=1[{label}];");
                sfxLabels.Add(label);
            }

            if (sfxLabels.Count > 0)
            {
                filter.Append($"{Label(baseAudioLabel)}");
                foreach (var label in sfxLabels) filter.Append(Label(label));
                filter.Append($"amix=inputs={sfxLabels.Count + 1}:duration=first:dropout_transition=0:normalize=0[a_mixed];");
            }
            else
            {
                filter.Append($"{Label(baseAudioLabel)}anull[a_mixed];");
            }

            var targetDuration = Math.Max(0.05, layout.TotalDuration);
            filter.Append($"[a_mixed]alimiter=limit=0.98,apad=whole_dur={FloatStr(targetDuration)}[a_limited];");

            if (mix.FadeAudioInOut)
            {
                string d = FloatStr(mix.FadeDurationSec);
                string fadeOutStart = FloatStr(Math.Max(0, layout.TotalDuration - mix.FadeDurationSec));
                filter.Append($"[a_limited]afade=t=in:st=0:d={d},afade=t=out:st={fadeOutStart}:d={d}[a_final]");
            }
            else
            {
                filter.Append($"[a_limited]anull[a_final]");
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
            var audioBitrate = Math.Clamp(mix.FinalAudioBitrateKbps, 96, 384);
            sb.Append($"-c:a aac -b:a {audioBitrate}k ");
            sb.Append($"-t {FloatStr(targetDuration)} ");
            sb.Append($"-y \"{outputPath}\"");

            return sb.ToString();
        }

        private static double GetEffectiveTransitionDuration(VisualEvent current, VisualEvent previous)
        {
            var transition = NormalizeXfadeTransition(current.TransitionType);
            if (string.IsNullOrWhiteSpace(transition)) return 0;

            var requested = current.TransitionDuration > 0 ? current.TransitionDuration : 0.35;
            var maxSafe = Math.Min(previous.Duration, current.Duration) * 0.45;
            if (maxSafe < 0.08) return 0;

            return Math.Clamp(requested, 0.08, Math.Min(1.5, maxSafe));
        }

        private static bool ShouldUseVoiceTimeline(List<AudioEvent> voiceEvents)
            => voiceEvents.Count > 0
               && voiceEvents.Any(v =>
                   Math.Abs(v.EditOffsetSec) > 0.01
                   || v.SourceStartOffsetSec > 0.01
                   || !string.IsNullOrWhiteSpace(v.EditTransition)
                      && !string.Equals(v.EditTransition, "straight", StringComparison.OrdinalIgnoreCase));

        private static bool AudioEventOverlapsChunk(AudioEvent audio, double chunkStart, double chunkEnd)
        {
            var duration = audio.Duration > 0 ? audio.Duration : GetFallbackAudioEventDuration(audio);
            var end = audio.StartTime + duration;

            return end > chunkStart - 0.01 && audio.StartTime < chunkEnd + 0.01;
        }

        private static double GetFallbackAudioEventDuration(AudioEvent audio)
        {
            if (string.Equals(audio.Type, "sfx", StringComparison.OrdinalIgnoreCase))
                return GetSfxCueDuration(audio.FilePath);

            return 5.0;
        }

        private static string? NormalizeXfadeTransition(string? transitionType)
        {
            var token = (transitionType ?? "")
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");

            return token switch
            {
                "crossfade" or "fade" => "fade",
                "dip_black" or "dip_to_black" or "fadeblack" => "fadeblack",
                "flash" or "flash_white" or "fadewhite" => "fadewhite",
                _ => null
            };
        }

        private static string EscapeDrawText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace(",", "\\,")
                .Replace(":", "\\:")
                // FFmpeg single-quoted filter values cannot contain a raw apostrophe.
                // Use: 'foo'\''bar' so the surrounding quote closes, emits an escaped apostrophe, then reopens.
                .Replace("'", "'\\''")
                .Replace("%", "\\%")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Trim();
        }

        private static string EscapeFilterExpression(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            return value
                .Replace("\\", "\\\\")
                .Replace(",", "\\,")
                .Replace(";", "\\;");
        }

        private static bool IsSyntheticSfx(string? filePath)
            => filePath?.StartsWith("synth://", StringComparison.OrdinalIgnoreCase) == true;

        private static string BuildSyntheticSfxSource(string filePath)
        {
            var cue = filePath["synth://".Length..]
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");

            return cue switch
            {
                "low_boom" or "boom" => "sine=frequency=62:duration=0.48:sample_rate=44100",
                "whoosh" or "swoosh" => "anoisesrc=color=pink:duration=0.38:sample_rate=44100",
                "hit" or "impact" => "sine=frequency=190:duration=0.18:sample_rate=44100",
                _ => "sine=frequency=160:duration=0.20:sample_rate=44100"
            };
        }

        private static double GetSfxCueDuration(string filePath)
        {
            if (!IsSyntheticSfx(filePath)) return 0.35;

            var cue = filePath["synth://".Length..]
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");

            return cue switch
            {
                "low_boom" or "boom" => 0.48,
                "whoosh" or "swoosh" => 0.38,
                "hit" or "impact" => 0.18,
                _ => 0.20
            };
        }

        private static async Task LogRenderCompilerSummaryAsync(SceneLayoutStagePayload layout, Func<string, Task>? logAsync)
        {
            var transitionCount = layout.VisualTrack
                .Skip(1)
                .Count(v => !string.IsNullOrWhiteSpace(NormalizeXfadeTransition(v.TransitionType)));
            var overlayCount = layout.VisualTrack.Count(v => !string.IsNullOrWhiteSpace(v.OverlayText));
            var appliedOverlayCount = layout.Style.VisualEffectsSettings.EnableOverlayText ? overlayCount : 0;
            var skippedOverlayCount = overlayCount - appliedOverlayCount;
            var sfxCount = layout.AudioTrack.Count(a => string.Equals(a.Type, "sfx", StringComparison.OrdinalIgnoreCase));
            var audioEditCount = layout.AudioTrack.Count(a =>
                string.Equals(a.Type, "voice", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(a.EditTransition)
                && !string.Equals(a.EditTransition, "straight", StringComparison.OrdinalIgnoreCase));

            if (transitionCount == 0 && overlayCount == 0 && sfxCount == 0 && audioEditCount == 0) return;

            await SafeLogAsync(
                logAsync,
                $"Render compiler: {transitionCount} geçiş, {appliedOverlayCount} overlay text, {sfxCount} audio cue, {audioEditCount} J/L-cut ses geçişi uygulanacak. Overlay atlanan: {skippedOverlayCount}.");
        }

        private static (string Zoom, string X, string Y) BuildMotionExpressions(string effectType, double maxZoom, int frames)
        {
            string FloatStr(double val) => val.ToString("0.00", CultureInfo.InvariantCulture);

            var progressDenominator = Math.Max(1, frames - 1);
            var centerX = "iw/2-(iw/zoom/2)";
            var centerY = "ih/2-(ih/zoom/2)";
            var panZoom = Math.Max(maxZoom, 1.08);
            var panZoomExpr = FloatStr(panZoom);
            var maxZoomExpr = FloatStr(maxZoom);
            var zoomDeltaExpr = FloatStr(Math.Max(0, maxZoom - 1.0));

            return effectType switch
            {
                "static" or "static_hold" => (
                    "1.0",
                    centerX,
                    centerY),
                "zoom_out" => (
                    $"{maxZoomExpr}-({zoomDeltaExpr}*on/{progressDenominator})",
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
                    $"1.0+({zoomDeltaExpr}*on/{progressDenominator})",
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

        private async Task RunFFmpegProcessAsync(
            string args,
            CancellationToken ct,
            double? progressTotalSeconds = null,
            Func<double, Task>? progressAsync = null)
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
            var errLogLock = new object();
            var lastProgressAt = DateTimeOffset.MinValue;
            var lastProgressPercent = -1d;

            p.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                lock (errLogLock)
                {
                    errLog.AppendLine(e.Data);
                }

                var totalSeconds = progressTotalSeconds.GetValueOrDefault();
                if (totalSeconds <= 0 || progressAsync == null)
                    return;

                if (!TryParseFfmpegProgressSeconds(e.Data, out var currentSeconds))
                    return;

                var percent = Math.Clamp((currentSeconds / totalSeconds) * 100, 0, 100);
                var now = DateTimeOffset.UtcNow;
                if (percent < 100
                    && now - lastProgressAt < TimeSpan.FromMilliseconds(650)
                    && percent - lastProgressPercent < 0.75)
                {
                    return;
                }

                lastProgressAt = now;
                lastProgressPercent = percent;
                _ = SafeProgressCallbackAsync(progressAsync, currentSeconds);
            };
            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0)
            {
                string logStr;
                lock (errLogLock)
                {
                    logStr = errLog.ToString();
                }
                var errPart = BuildFFmpegErrorExcerpt(logStr);
                throw new Exception($"FFmpeg Hatası: {errPart}");
            }
        }

        private static Func<double, Task>? CreateRenderProgressReporter(
            Func<RenderProgressUpdate, Task>? progressAsync,
            double operationTotalSeconds,
            double totalRenderSeconds,
            double startPercent,
            double endPercent,
            string label,
            int? chunkIndex,
            int? totalChunks)
        {
            if (progressAsync == null || operationTotalSeconds <= 0 || totalRenderSeconds <= 0)
                return null;

            var safeStart = Math.Clamp(startPercent, 0, 100);
            var safeEnd = Math.Clamp(endPercent, safeStart, 100);
            var span = safeEnd - safeStart;

            return currentSeconds =>
            {
                var ratio = Math.Clamp(currentSeconds / operationTotalSeconds, 0, 1);
                var percent = safeStart + (ratio * span);
                var roundedPercent = Math.Round(percent, 1);
                var totalCurrentSeconds = totalRenderSeconds * (roundedPercent / 100);

                return SafeRenderProgressAsync(progressAsync, new RenderProgressUpdate
                {
                    Label = label,
                    Percent = roundedPercent,
                    CurrentSeconds = totalCurrentSeconds,
                    TotalSeconds = totalRenderSeconds,
                    ChunkIndex = chunkIndex,
                    TotalChunks = totalChunks
                });
            };
        }

        private static bool TryParseFfmpegProgressSeconds(string line, out double seconds)
        {
            seconds = 0;
            var match = FfmpegTimeRegex.Match(line);
            if (!match.Success) return false;

            if (!int.TryParse(match.Groups["hours"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
                return false;
            if (!int.TryParse(match.Groups["minutes"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
                return false;
            if (!double.TryParse(match.Groups["seconds"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSeconds))
                return false;

            seconds = (hours * 3600) + (minutes * 60) + parsedSeconds;
            return seconds >= 0;
        }

        private static async Task SafeProgressCallbackAsync(Func<double, Task> progressAsync, double currentSeconds)
        {
            try
            {
                await progressAsync(currentSeconds);
            }
            catch
            {
                // Progress bildirimi FFmpeg akışını bozmasın.
            }
        }

        private static async Task SafeRenderProgressAsync(Func<RenderProgressUpdate, Task>? progressAsync, RenderProgressUpdate progress)
        {
            if (progressAsync == null) return;

            try
            {
                progress.Percent = Math.Clamp(progress.Percent, 0, 100);
                if (progress.TotalSeconds > 0)
                {
                    progress.CurrentSeconds = Math.Clamp(progress.CurrentSeconds, 0, progress.TotalSeconds);
                }

                progress.TimestampUtc = DateTime.UtcNow;
                await progressAsync(progress);
            }
            catch
            {
                // Progress bildirimi render akışını bozmasın.
            }
        }

        private static string BuildFFmpegErrorExcerpt(string log)
        {
            if (string.IsNullOrWhiteSpace(log)) return "FFmpeg detay logu boş döndü.";
            if (log.Length <= 9000) return log;

            var head = log[..3000];
            var tail = log[^5000..];
            return $"{head}\n--- FFmpeg log ortası kısaltıldı ---\n{tail}";
        }

        private static async Task EnsureOutputDurationAsync(string outputPath, double expectedDuration, CancellationToken ct)
        {
            if (expectedDuration <= 1 || !File.Exists(outputPath)) return;

            var actualDuration = await ProbeDurationAsync(outputPath, ct);
            if (actualDuration <= 0) return;

            var minExpected = Math.Max(1, expectedDuration * 0.92);
            if (actualDuration < minExpected)
            {
                throw new InvalidOperationException(
                    $"Render çıktısı beklenenden kısa. Beklenen: {expectedDuration:F1} sn, oluşan: {actualDuration:F1} sn.");
            }
        }

        private static async Task<double> ProbeDurationAsync(string mediaPath, CancellationToken ct)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=nw=1:nk=1 \"{mediaPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();
            var output = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            return double.TryParse(
                output.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var duration)
                ? duration
                : 0;
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
