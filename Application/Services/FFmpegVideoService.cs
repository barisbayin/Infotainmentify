using Application.Abstractions;
using Application.Models;
using System.Diagnostics;
using System.Text;

namespace Application.Services
{
    public class FFmpegVideoService : IVideoRendererService
    {
        public async Task<string> RenderVideoAsync(SceneLayoutStagePayload layout, string outputPath, CancellationToken ct = default)
        {
            var tempGuid = Guid.NewGuid().ToString();
            var srtPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $"subs_{tempGuid}.srt");
            var audioListPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $"audio_list_{tempGuid}.txt");

            // 🎵 1. Müzik Dosyasını Bul (Rastgele veya Sabit)
            var musicPath = GetRandomMusicFile();

            try
            {
                // Altyazı ve Ses Listesi Oluştur
                await GenerateSrtFileAsync(layout.CaptionTrack, srtPath);
                await GenerateAudioListAsync(layout.AudioTrack, audioListPath);

                // FFmpeg Komutunu Kur
                var ffmpegArgs = BuildFFmpegCommand(layout, srtPath, audioListPath, musicPath, outputPath);

                // Çalıştır
                await RunFFmpegProcessAsync(ffmpegArgs, ct);

                return outputPath;
            }
            finally
            {
                // Temizlik
                if (File.Exists(srtPath)) File.Delete(srtPath);
                if (File.Exists(audioListPath)) File.Delete(audioListPath);
            }
        }

        // --- MÜZİK SEÇİCİ ---
        private string? GetRandomMusicFile()
        {
            // Projende "wwwroot/assets/music" klasörü oluşturup içine mp3 atman lazım!
            var musicDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets", "Music");
            if (!Directory.Exists(musicDir)) return null;

            var files = Directory.GetFiles(musicDir, "*.mp3");
            if (files.Length == 0) return null;

            // Rastgele birini seç
            return files[new Random().Next(files.Length)];
        }

        // --- FFmpeg KOMUT İNŞASI (BÜYÜK GÜNCELLEME) ---
        private string BuildFFmpegCommand(SceneLayoutStagePayload layout, string srtPath, string audioListPath, string? musicPath, string outputPath)
        {
            var sb = new StringBuilder();
            var filter = new StringBuilder();

            // ---------------------------------------------------------
            // 1. INPUTLAR
            // ---------------------------------------------------------
            // [0] Ses Listesi (Konuşmalar)
            sb.Append($"-f concat -safe 0 -i \"{audioListPath}\" ");

            // [1] Müzik (Eğer varsa) - Loop parametresiyle
            if (musicPath != null)
            {
                sb.Append($"-stream_loop -1 -i \"{musicPath}\" ");
            }

            // [2..N] Resimler
            int imgStartIndex = musicPath != null ? 2 : 1;
            foreach (var visual in layout.VisualTrack)
            {
                sb.Append($"-i \"{visual.ImagePath}\" ");
            }

            // ---------------------------------------------------------
            // 2. FILTER COMPLEX (VİDEO İŞLEME)
            // ---------------------------------------------------------
            sb.Append("-filter_complex \"");

            // --- Zoom/Pan Efektleri (Supersampling ile Titreşimsiz) ---
            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                var v = layout.VisualTrack[i];
                var inputIdx = imgStartIndex + i;

                // Zoom Efekti Hesabı
                string zoomExpr = v.EffectType == "zoom_in"
                    ? "min(zoom+0.0015,1.5)"
                    : "if(eq(on,1),1.5,max(1.0,zoom-0.0015))";

                int frameCount = (int)(v.Duration * layout.Fps);

                // Zincir: [Resim] -> Scale(8K) -> Zoompan -> Scale(Final) -> [v{i}]
                // scale=8000:-1 : Titreşimi önlemek için devasa büyütme
                filter.Append($"[{inputIdx}:v]scale=8000:-1,zoompan=z='{zoomExpr}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d={frameCount}:s={layout.Width}x{layout.Height}:fps={layout.Fps},setsar=1[v{i}];");
            }

            // --- Concat (Videoları Birleştirme) ---
            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                filter.Append($"[v{i}]");
            }
            filter.Append($"concat=n={layout.VisualTrack.Count}:v=1:a=0[vbase];");

            // ---------------------------------------------------------
            // 3. ALTYAZI (PRESET'TEN GELEN DİNAMİK AYARLAR)
            // ---------------------------------------------------------
            var safeSrt = srtPath.Replace("\\", "/").Replace(":", "\\:");

            // Dinamik Font Boyutu ve Margin Hesabı
            // MarginV = Font Boyutu + 20px (Yazı büyüdükçe yukarı kaysın ki kesilmesin)
            int fontSize = layout.Style.FontSize;
            int marginV = fontSize + 20;

            // force_style ile veritabanındaki font boyutunu basıyoruz
            filter.Append($"[vbase]subtitles='{safeSrt}':force_style='FontName=Arial,Alignment=2,MarginV={marginV},Fontsize={fontSize},BorderStyle=3,Outline=0,Shadow=0,PrimaryColour=&H00FFFFFF,BackColour=&H80000000'[vfinal];");

            // ---------------------------------------------------------
            // 4. SES MİKSAJI (PRESET'TEN GELEN DİNAMİK SES)
            // ---------------------------------------------------------
            if (musicPath != null)
            {
                // Ducking açıksa %15 (0.15) sabit, kapalıysa kullanıcının seçtiği (layout.Style.MusicVolume)
                double volume = layout.Style.IsDuckingEnabled ? 0.15 : layout.Style.MusicVolume;

                // Nokta/Virgül hatası olmasın diye InvariantCulture
                string volStr = volume.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                filter.Append($"[1:a]volume={volStr}[bg];[0:a][bg]amix=inputs=2:duration=first[afinal]\"");
            }
            else
            {
                // Müzik yoksa sadece sesi passthrough yap
                filter.Append($"[0:a]anull[afinal]\"");
            }

            // ---------------------------------------------------------
            // 5. ÇIKTI AYARLARI (PRESET'TEN GELEN KALİTE)
            // ---------------------------------------------------------
            sb.Append(filter.ToString());
            sb.Append(" -map \"[vfinal]\" -map \"[afinal]\" ");

            // Encoder Preset (Hız): ultrafast, medium, slow (Veritabanından)
            sb.Append($"-c:v libx264 -pix_fmt yuv420p -preset {layout.Style.EncoderPreset} ");

            // Bitrate (Kalite): 6000k, 8000k (Veritabanından)
            // CRF yerine sabit bitrate kullanıyoruz
            sb.Append($"-b:v {layout.Style.BitrateKbps}k ");

            sb.Append("-c:a aac -b:a 192k ");
            sb.Append("-shortest ");
            sb.Append($"-y \"{outputPath}\"");

            return sb.ToString();
        }

        // --- ALTYAZI OLUŞTURUCU (UTF-8 BOM FIX) ---
        private async Task GenerateSrtFileAsync(List<CaptionEvent> captions, string path)
        {
            var sb = new StringBuilder();
            int index = 1;

            foreach (var cap in captions)
            {
                sb.AppendLine(index.ToString());
                // Zamanlar arasında çakışma olmaması için milisaniyeye dikkat
                var start = TimeSpan.FromSeconds(cap.Start).ToString(@"hh\:mm\:ss\,fff");
                var end = TimeSpan.FromSeconds(cap.End).ToString(@"hh\:mm\:ss\,fff");

                sb.AppendLine($"{start} --> {end}");
                sb.AppendLine(cap.Text);
                sb.AppendLine();
                index++;
            }

            // Türkçe karakterler için UTF8 Encoding (BOM'suz veya BOM'lu, FFmpeg genelde çözer ama UTF8 şart)
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        // --- SES LİSTESİ ---
        // --- SES LİSTESİ (Concat Demuxer) ---
        private async Task GenerateAudioListAsync(List<AudioEvent> audios, string path)
        {
            var sb = new StringBuilder();

            // Sadece 'voice' (konuşma) olanları al ve zamana göre sırala
            foreach (var audio in audios.Where(a => a.Type == "voice").OrderBy(a => a.StartTime))
            {
                // Windows path'leri (ters slash) FFmpeg listesinde sorun çıkarır, düzeltiyoruz
                var safePath = audio.FilePath.Replace("\\", "/");
                sb.AppendLine($"file '{safePath}'");
            }

            // 🔥 DÜZELTME BURADA:
            // Encoding.UTF8 yerine 'new UTF8Encoding(false)' kullanıyoruz.
            // Bu sayede dosyanın başına o gizli BOM karakterlerini koymuyor.
            // FFmpeg artık dosyayı tertemiz okuyacak.
            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
        }

        // --- FFmpeg PROCESS ---
        private async Task RunFFmpegProcessAsync(string arguments, CancellationToken ct)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var errorLog = new StringBuilder();
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorLog.AppendLine(e.Data); };

            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                // Hata alırsan logun sonunu göster
                var logStr = errorLog.ToString();
                throw new Exception($"FFmpeg Hatası: {logStr.Substring(Math.Max(0, logStr.Length - 1000))}");
            }
        }
    }
}
