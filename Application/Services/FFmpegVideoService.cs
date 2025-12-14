using Application.Abstractions;
using Application.Models;
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
            // wwwroot/ALL_FILES/Assets yolunu belirliyoruz
            _assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets");
        }

        // 🔥 GÜNCELLEME: cultureCode parametresi eklendi
        public async Task<string> RenderVideoAsync(SceneLayoutStagePayload layout, string outputPath, string cultureCode = "en-US", CancellationToken ct = default)
        {
            var tempId = Guid.NewGuid().ToString("N")[..8];
            var workDir = Path.GetDirectoryName(outputPath)!;

            // Geçici dosya yolları
            var srtPath = Path.Combine(workDir, $"subs_{tempId}.srt");
            var audioListPath = Path.Combine(workDir, $"audio_{tempId}.txt");

            // 🎵 Müzik Seçimi (Layout'ta yoksa rastgele)
            var musicPath = GetRandomMusicFile();

            try
            {
                // 1. Altyazı Dosyasını Oluştur (.srt)
                // 🔥 Dil kodunu ve Ayarları gönderiyoruz
                await GenerateSrtFileAsync(layout.CaptionTrack, srtPath, layout.Style.CaptionSettings, cultureCode);

                // 2. Ses Listesini Oluştur (.txt concat formatı)
                await GenerateAudioListAsync(layout.AudioTrack, audioListPath);

                // 3. FFmpeg Komutunu İnşa Et
                var args = BuildFFmpegCommand(layout, srtPath, audioListPath, musicPath, outputPath);

                // 4. Render'ı Başlat
                await RunFFmpegProcessAsync(args, ct);

                return outputPath;
            }
            finally
            {
                // Çöp dosyaları temizle
                if (File.Exists(srtPath)) File.Delete(srtPath);
                if (File.Exists(audioListPath)) File.Delete(audioListPath);
            }
        }

        // -----------------------------------------------------------------------
        // 🔥 FFmpeg KOMUT İNŞASI (DÜZELTİLMİŞ & OPTİMİZE EDİLMİŞ)
        // -----------------------------------------------------------------------
        private string BuildFFmpegCommand(SceneLayoutStagePayload layout, string srtPath, string audioListPath, string? musicPath, string outputPath)
        {
            var sb = new StringBuilder();
            var filter = new StringBuilder();

            // Helper: Windows yollarını FFmpeg formatına çevir
            string Escape(string p) => p.Replace("\\", "/").Replace(":", "\\:");
            string FloatStr(double val) => val.ToString("0.00", CultureInfo.InvariantCulture);

            // ==========================================
            // 1. GİRDİLER (INPUTS)
            // ==========================================

            // [0] Ses Dosyaları Listesi (Konuşmalar)
            sb.Append($"-f concat -safe 0 -i \"{audioListPath}\" ");

            // [1] Arka Plan Müziği (Varsa)
            if (musicPath != null) sb.Append($"-stream_loop -1 -i \"{musicPath}\" ");

            // [2..N] Görsel Dosyalar (Resimler)
            int imgStartIndex = musicPath != null ? 2 : 1;
            foreach (var visual in layout.VisualTrack)
            {
                sb.Append($"-i \"{visual.ImagePath}\" ");
            }

            // ==========================================
            // 2. FILTRE ZİNCİRİ (FILTER COMPLEX)
            // ==========================================
            sb.Append("-filter_complex \"");

            // --- A) GÖRSEL EFEKTLER (ZOOM FIX YAPILDI) ---
            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                var v = layout.VisualTrack[i];
                int idx = imgStartIndex + i;
                int frames = (int)(v.Duration * layout.Fps);

                double maxZoom = layout.Style.VisualEffectsSettings.ZoomIntensity;
                if (maxZoom < 1.0) maxZoom = 1.1; // Default güvenlik

                // 🔥 init_zoom Kaldırıldı, Formül revize edildi 🔥
                string zoomExpr;
                if (v.EffectType == "zoom_in")
                {
                    // Zoom In: 1'den başla -> maxZoom'a git
                    zoomExpr = $"min(zoom+0.0015,{FloatStr(maxZoom)})";
                }
                else // zoom_out
                {
                    // Zoom Out: İlk karede (on=1) maxZoom yap, sonra azalt
                    zoomExpr = $"if(eq(on,1),{FloatStr(maxZoom)},max(1.0,zoom-0.0015))";
                }

                // High Quality Zoom için önce scale=-2:4*ih yapıyoruz
                filter.Append($"[{idx}:v]scale=-2:4*ih,zoompan=z='{zoomExpr}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d={frames}:s={layout.Width}x{layout.Height}:fps={layout.Fps},setsar=1");

                // Renk Filtreleri
                var cf = layout.Style.VisualEffectsSettings.ColorFilter;
                if (!string.IsNullOrEmpty(cf))
                {
                    if (cf == "bw_noir") filter.Append(",hue=s=0");
                    else if (cf == "cinematic_warm") filter.Append(",curves=r='0/0 1/1':g='0/0 0.8/1':b='0/0 0.8/1'");
                }

                filter.Append($"[v{i}];");
            }

            // --- B) CONCAT ---
            for (int i = 0; i < layout.VisualTrack.Count; i++) filter.Append($"[v{i}]");
            filter.Append($"concat=n={layout.VisualTrack.Count}:v=1:a=0[v_joined];");

            // --- C) BRANDING / WATERMARK ---
            string vAfterBranding = "[v_branded]";
            var branding = layout.Style.BrandingSettings;

            if (branding != null && branding.EnableWatermark)
            {
                string overlayPos = branding.Position switch
                {
                    "TopLeft" => "x=20:y=20",
                    "TopRight" => "x=W-w-20:y=20",
                    "BottomLeft" => "x=20:y=H-h-20",
                    _ => "x=W-w-20:y=H-h-20" // Default BottomRight
                };

                string fontPath = GetFontPath("Arial-Bold");
                string colorAss = HexToAssColor(branding.WatermarkColor, branding.Opacity);

                filter.Append($"[v_joined]drawtext=fontfile='{Escape(fontPath)}':text='{branding.WatermarkText}':fontcolor={colorAss}:fontsize=24:{overlayPos}[v_branded];");
            }
            else
            {
                vAfterBranding = "[v_joined]";
            }

            string lastVideoLabel = (branding != null && branding.EnableWatermark) ? "[v_branded]" : "[v_joined]";

            // --- D) ALTYAZILAR (GELİŞMİŞ) ---
            var capSettings = layout.Style.CaptionSettings;
            if (capSettings.EnableCaptions)
            {
                string srt = Escape(srtPath);
                string fontFile = GetFontPath(capSettings.FontName);
                string fontsDir = Escape(Path.GetDirectoryName(fontFile)!);

                string primary = HexToAssColor(capSettings.PrimaryColor);
                string outline = HexToAssColor(capSettings.OutlineColor);

                // Pozisyon Ayarı (Alignment)
                int alignment = capSettings.Position switch
                {
                    CaptionPositionTypes.Bottom => 2, // Alt Orta
                    CaptionPositionTypes.Center => 5, // Tam Orta
                    CaptionPositionTypes.Top => 6,    // Üst Orta
                    _ => 2
                };

                // BorderStyle=1 (Outline) default. Highlight istenirse BorderStyle=3 (Box)
                string borderStyle = "1";
                string outlineParam = $"OutlineColour={outline}";

                if (capSettings.EnableHighlight)
                {
                    borderStyle = "3"; // Kutu
                    // Highlight rengini OutlineColour yerine BackColour olarak veriyoruz (veya OutlineColour kutu rengi olur)
                    // ASS'de BackColour kutu rengidir.
                    string highlightColor = HexToAssColor(capSettings.HighlightColor ?? "#000000", 0.7);
                    outlineParam = $"BackColour={highlightColor},OutlineColour={outline}";
                }

                string style = $"FontName={Path.GetFileNameWithoutExtension(fontFile)},FontSize={capSettings.FontSize},PrimaryColour={primary},{outlineParam},BorderStyle={borderStyle},Outline={capSettings.OutlineSize},Shadow=0,MarginV={capSettings.MarginBottom},Alignment={alignment}";

                filter.Append($"{lastVideoLabel}subtitles='{srt}':fontsdir='{fontsDir}':force_style='{style}'[v_final];");
            }
            else
            {
                filter.Append($"{lastVideoLabel}null[v_final];");
            }

            // --- E) SES MİKSAJI (AUDIO DUCKING & FADE) ---
            var mix = layout.Style.AudioMixSettings;
            if (musicPath != null)
            {
                double musicVol = mix.MusicVolumePercent / 100.0;
                double voiceVol = mix.VoiceVolumePercent / 100.0;

                filter.Append($"[1:a]volume={FloatStr(musicVol)}[bg];");
                filter.Append($"[0:a]volume={FloatStr(voiceVol)}[fg];");

                if (mix.EnableDucking)
                {
                    // Ducking: Müzik kısılsın konuşma gelince
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

            // ==========================================
            // 3. ÇIKTI AYARLARI (GPU & ENCODER)
            // ==========================================
            sb.Append(filter.ToString());
            sb.Append(" -map \"[v_final]\" -map \"[a_final]\" ");

            // GPU Preset
            string gpuPreset = layout.Style.EncoderPreset switch
            {
                "ultrafast" or "superfast" => "p1",
                "fast" => "p3",
                "slow" => "p6",
                _ => "p4"
            };

            sb.Append($"-c:v h264_nvenc -preset {gpuPreset} ");
            sb.Append($"-b:v {layout.Style.BitrateKbps}k -maxrate {layout.Style.BitrateKbps + 1000}k -bufsize 10M ");
            sb.Append("-pix_fmt yuv420p ");
            sb.Append("-c:a aac -b:a 192k ");
            sb.Append("-shortest "); // En kısa olana göre kes (Genelde ses biter video biter)
            sb.Append($"-y \"{outputPath}\"");

            return sb.ToString();
        }

        // -----------------------------------------------------------------------
        // 🛠️ YARDIMCI METODLAR
        // -----------------------------------------------------------------------

        private string GetFontPath(string fontName)
        {
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
            if (hex.Length >= 6)
            {
                r = hex.Substring(0, 2);
                g = hex.Substring(2, 2);
                b = hex.Substring(4, 2);
            }

            // Alpha Tersi (00 görünür, FF saydam)
            int alphaInt = (int)((1.0 - opacity) * 255);
            string alphaHex = alphaInt.ToString("X2");

            return $"&H{alphaHex}{b}{g}{r}"; // BGR sıralaması
        }

        private string? GetRandomMusicFile()
        {
            var musicDir = Path.Combine(_assetsPath, "music");
            if (!Directory.Exists(musicDir)) return null;
            var files = Directory.GetFiles(musicDir, "*.mp3");
            return files.Length > 0 ? files[new Random().Next(files.Length)] : null;
        }

        // 🔥 GÜNCELLENMİŞ SRT METODU (Settings & Culture)
        private async Task GenerateSrtFileAsync(List<CaptionEvent> captions, string path, RenderCaptionSettings settings, string cultureCode)
        {
            var sb = new StringBuilder();
            int i = 1;

            // CultureInfo oluştur (Fallback: en-US)
            CultureInfo culture;
            try { culture = new CultureInfo(cultureCode); }
            catch { culture = new CultureInfo("en-US"); }

            foreach (var cap in captions)
            {
                string text = cap.Text;

                // 1. Uppercase Ayarı
                if (settings.Uppercase)
                {
                    text = text.ToUpper(culture);
                }

                // 2. Satır Bölme (MaxWordsPerLine)
                if (settings.MaxWordsPerLine > 0 && settings.MaxWordsPerLine < 20)
                {
                    text = BreakTextToLines(text, settings.MaxWordsPerLine);
                }

                sb.AppendLine($"{i++}");
                sb.AppendLine($"{TimeSpan.FromSeconds(cap.Start):hh\\:mm\\:ss\\,fff} --> {TimeSpan.FromSeconds(cap.End):hh\\:mm\\:ss\\,fff}");
                sb.AppendLine(text);
                sb.AppendLine();
            }
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        private string BreakTextToLines(string text, int maxWords)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var words = text.Split(' ');
            if (words.Length <= maxWords) return text;

            var sb = new StringBuilder();
            int count = 0;
            foreach (var word in words)
            {
                sb.Append(word + " ");
                count++;
                if (count >= maxWords)
                {
                    sb.AppendLine();
                    count = 0;
                }
            }
            return sb.ToString().Trim();
        }

        private async Task GenerateAudioListAsync(List<AudioEvent> audios, string path)
        {
            var sb = new StringBuilder();
            foreach (var audio in audios.Where(a => a.Type == "voice").OrderBy(a => a.StartTime))
            {
                // URL ise de çalışmazsa Executor'da çevirdik varsayıyoruz.
                // Yine de garanti olsun diye Replace yapıyoruz ama fiziksel yol gelmeli.
                sb.AppendLine($"file '{audio.FilePath.Replace("\\", "/")}'");
            }
            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
        }

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
                // Son 1000 karakteri göster
                var errPart = logStr.Length > 1000 ? logStr.Substring(logStr.Length - 1000) : logStr;
                throw new Exception($"FFmpeg Hatası: {errPart}");
            }
        }
    }
}
