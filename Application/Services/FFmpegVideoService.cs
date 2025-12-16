using Application.Abstractions;
using Application.Models;
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
        private readonly string _assetsPath;

        public FFmpegVideoService()
        {
            _assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets");
        }

        public async Task<string> RenderVideoAsync(SceneLayoutStagePayload layout, string outputPath, string cultureCode = "en-US", CancellationToken ct = default)
        {
            var tempId = Guid.NewGuid().ToString("N")[..8];
            var workDir = Path.GetDirectoryName(outputPath)!;

            // Geçici dosya yolları
            var srtPath = Path.Combine(workDir, $"subs_{tempId}.ass"); // ASS uzantısı önemli
            var audioListPath = Path.Combine(workDir, $"audio_{tempId}.txt");

            var musicPath = GetRandomMusicFile();

            try
            {
                // 1. ASS Altyazı Dosyasını Oluştur (Stil ve Animasyonlu)
                await GenerateDynamicAssFileAsync(layout.CaptionTrack, srtPath, layout.Style.CaptionSettings, cultureCode);

                // 2. Ses Listesini Oluştur
                await GenerateAudioListAsync(layout.AudioTrack, audioListPath);

                // 3. FFmpeg Komutunu İnşa Et
                var args = BuildFFmpegCommand(layout, srtPath, audioListPath, musicPath, outputPath);

                // 4. Render'ı Başlat
                await RunFFmpegProcessAsync(args, ct);

                return outputPath;
            }
            finally
            {
                if (File.Exists(srtPath)) File.Delete(srtPath);
                if (File.Exists(audioListPath)) File.Delete(audioListPath);
            }
        }

        // ... (BuildFFmpegCommand, GetFontPath, HexToAssColor, GetRandomMusicFile metodları AYNI KALSIN) ...
        // Yer kaplamasın diye onları tekrar yazmıyorum, yukarıdaki kodunun aynısı.
        // Sadece BuildFFmpegCommand metodunun içini atıyorum tekrar kontrol etmen için:

        private string BuildFFmpegCommand(SceneLayoutStagePayload layout, string subPath, string audioListPath, string? musicPath, string outputPath)
        {
            var sb = new StringBuilder();
            var filter = new StringBuilder();

            string Escape(string p) => p.Replace("\\", "/").Replace(":", "\\:");
            string FloatStr(double val) => val.ToString("0.00", CultureInfo.InvariantCulture);

            // GİRDİLER
            sb.Append($"-f concat -safe 0 -i \"{audioListPath}\" ");
            if (musicPath != null) sb.Append($"-stream_loop -1 -i \"{musicPath}\" ");

            int imgStartIndex = musicPath != null ? 2 : 1;
            foreach (var visual in layout.VisualTrack)
            {
                sb.Append($"-i \"{visual.ImagePath}\" ");
            }

            // FILTER COMPLEX
            sb.Append("-filter_complex \"");

            // A) GÖRSELLER (Zoom/Pan)
            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                var v = layout.VisualTrack[i];
                int idx = imgStartIndex + i;
                int frames = (int)(v.Duration * layout.Fps);

                double maxZoom = layout.Style.VisualEffectsSettings.ZoomIntensity;
                if (maxZoom < 1.0) maxZoom = 1.1;

                string zoomExpr;
                if (v.EffectType == "zoom_in") zoomExpr = $"min(zoom+0.0015,{FloatStr(maxZoom)})";
                else zoomExpr = $"if(eq(on,1),{FloatStr(maxZoom)},max(1.0,zoom-0.0015))";

                filter.Append($"[{idx}:v]scale=-2:4*ih,zoompan=z='{zoomExpr}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d={frames}:s={layout.Width}x{layout.Height}:fps={layout.Fps},setsar=1");

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
                string fontPath = GetFontPath("Arial-Bold");
                string colorAss = HexToAssColor(branding.WatermarkColor, branding.Opacity);
                filter.Append($"[v_joined]drawtext=fontfile='{Escape(fontPath)}':text='{branding.WatermarkText}':fontcolor={colorAss}:fontsize=24:{overlayPos}[v_branded];");
                lastVideoLabel = "[v_branded]";
            }

            // D) ALTYAZILAR (ASS - Stil Dosyanın İçinde)
            var capSettings = layout.Style.CaptionSettings;
            if (capSettings.EnableCaptions)
            {
                string assFile = Escape(subPath);
                string fontFile = GetFontPath(capSettings.FontName);
                string fontsDir = Escape(Path.GetDirectoryName(fontFile)!);

                // Force Style SİLİNDİ, sadece dosya yolu ve font klasörü veriliyor.
                filter.Append($"{lastVideoLabel}subtitles='{assFile}':fontsdir='{fontsDir}'[v_final];");
            }
            else
            {
                filter.Append($"{lastVideoLabel}null[v_final];");
            }

            // E) SES
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

            // GPU
            string gpuPreset = layout.Style.EncoderPreset switch { "fast" => "p3", "slow" => "p6", _ => "p4" };
            sb.Append($"-c:v h264_nvenc -preset {gpuPreset} ");
            sb.Append($"-b:v {layout.Style.BitrateKbps}k -maxrate {layout.Style.BitrateKbps + 1000}k -bufsize 10M ");
            sb.Append("-pix_fmt yuv420p ");
            sb.Append("-c:a aac -b:a 192k ");
            sb.Append("-shortest ");
            sb.Append($"-y \"{outputPath}\"");

            return sb.ToString();
        }

        // -----------------------------------------------------------------------
        // 🔥 DÜZELTİLMİŞ ASS GENERATOR (OKUNABİLİRLİK İÇİN)
        // -----------------------------------------------------------------------
        private async Task GenerateDynamicAssFileAsync(List<CaptionEvent> captions, string path, RenderCaptionSettings settings, string cultureCode)
        {
            var sb = new StringBuilder();

            // Culture Handling
            CultureInfo culture;
            try { culture = new CultureInfo(cultureCode); } catch { culture = new CultureInfo("en-US"); }

            // 1. FONT VE OKUNABİLİRLİK AYARLARI
            int finalFontSize = settings.FontSize;
            if (finalFontSize < 75) finalFontSize = 75; // Okunabilirlik garantisi

            // --- HEADER ---
            sb.AppendLine("[Script Info]");
            sb.AppendLine("ScriptType: v4.00+");
            sb.AppendLine("WrapStyle: 2");
            sb.AppendLine("PlayResX: 1080");
            sb.AppendLine("PlayResY: 1920");
            sb.AppendLine();

            sb.AppendLine("[V4+ Styles]");
            sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");

            // RENKLER
            string primary = HexToAssColor(settings.PrimaryColor); // Örn: Beyaz

            // Eğer vurgu rengi Siyah ise (#000000), bunu yazı rengi yapmamalıyız!
            // Sadece arka plan rengi olarak kullanacağız.
            string highlightBoxColor = HexToAssColor(settings.HighlightColor ?? "#000000", 1.0);

            // Normal Outline rengi (Highlight kapalıyken)
            string normalOutlineColor = HexToAssColor(settings.OutlineColor);

            // Kutu Ayarları
            string backColor = normalOutlineColor;
            int borderStyle = 1;
            int outlineSize = settings.OutlineSize;

            // Harf Aralığı (Spacing): Kutu olunca harfler yapışmasın diye açıyoruz
            double spacing = 1.0;

            if (settings.EnableHighlight)
            {
                // 🔥 Kutu Rengi = Vurgu Rengi (Siyah)
                backColor = highlightBoxColor;

                // Outline Kalınlığı: Çok kalın olursa yazıyı yer. %15 idealdir.
                outlineSize = (int)(finalFontSize * 0.15);
                if (outlineSize < 5) outlineSize = 5;

                // Kutu modunda harfleri biraz daha ayır ki oval arka planlar birbirine girmesin
                spacing = 3.0;
            }

            string fontName = Path.GetFileNameWithoutExtension(settings.FontName);

            int align = settings.Position switch
            {
                CaptionPositionTypes.Bottom => 2,
                CaptionPositionTypes.Center => 5,
                CaptionPositionTypes.Top => 8,
                _ => 2
            };

            int marginV = settings.MarginBottom;
            if (align == 2 && marginV < 200) marginV = 200;

            // Stil Tanımı
            sb.AppendLine($"Style: Default,{fontName},{finalFontSize},{primary},&H000000FF,{backColor},&H00000000,1,0,0,0,100,100,{spacing},0,{borderStyle},{outlineSize},0,{align},20,20,{marginV},1");
            sb.AppendLine();

            sb.AppendLine("[Events]");
            sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            // --- KELİME İŞLEME VE MANTIĞI ---
            int targetWordCount = settings.MaxWordsPerLine > 0 ? settings.MaxWordsPerLine : 2;

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

            for (int i = 0; i < flatWordList.Count; i += targetWordCount)
            {
                var chunk = flatWordList.Skip(i).Take(targetWordCount).ToList();
                if (!chunk.Any()) break;

                string lineText = string.Join(" ", chunk.Select(c => c.Word));
                double startTimeMs = chunk.First().Start * 1000;
                double endTimeMs = chunk.Last().End * 1000;

                // 🔥 KRİTİK DÜZELTME: ANİMASYON TAGLERİ
                string animTags = "";
                if (settings.EnableHighlight)
                {
                    // ESKİSİ (Hatalı): {{\\1c{highlightBoxColor}...}} -> Yazıyı SİYAH yapıyordu.

                    // YENİSİ: Rengi değiştirme! Sadece Büyüt (Pop Up).
                    // Yazı zaten Primary (Beyaz) kalacak, Arka Plan (Box) zaten Siyah tanımlandı.
                    // \t(0,100,\fscx110\fscy110): İlk 100ms içinde %110 büyüt.
                    animTags = "{\\t(0,100,\\fscx110\\fscy110)}";

                    // İstersen opsiyonel: Vurgu rengi Siyah değilse (örn: Sarı), yazıyı Sarı yapabilirsin.
                    // Ama senin UI'da "Vurgu" genelde kutu rengi olarak algılandığı için 
                    // rengi değiştirmemek en güvenlisidir.
                }

                TimeSpan start = TimeSpan.FromMilliseconds(startTimeMs);
                TimeSpan end = TimeSpan.FromMilliseconds(endTimeMs);

                sb.AppendLine($"Dialogue: 0,{FormatAssTime(start)},{FormatAssTime(end)},Default,,0,0,0,,{animTags}{lineText}");
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        // Diğer helper metodlar (GetFontPath, HexToAssColor, BreakTextToLines, GenerateAudioListAsync, RunFFmpegProcessAsync, FormatAssTime)
        // Bunları zaten önceden yazmıştık, aynılarını kullanabilirsin.
        // Özellikle FormatAssTime metodunu class içine eklemeyi unutma.
        private string FormatAssTime(TimeSpan t) => $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds / 10:D2}";

        // ... (HexToAssColor vb. önceki koddan alabilirsin) ...
        private string GetFontPath(string fontName)
        {
            // Önceki implementasyonun aynısı
            string directPath = Path.Combine(_assetsPath, "fonts", fontName);
            if (File.Exists(directPath)) return directPath;
            string withExt = Path.Combine(_assetsPath, "fonts", fontName + ".ttf");
            if (File.Exists(withExt)) return withExt;
            var defaultFont = Directory.GetFiles(Path.Combine(_assetsPath, "fonts"), "*.ttf").FirstOrDefault();
            return defaultFont ?? "Arial";
        }

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
                sb.AppendLine($"file '{audio.FilePath.Replace("\\", "/")}'");
            }
            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
        }

        private async Task RunFFmpegProcessAsync(string args, CancellationToken ct)
        {
            // Önceki implementasyonun aynısı
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
    }
}
