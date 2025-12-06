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
            // Geçici dosya isimleri (Render bitince silinecekler)
            var tempGuid = Guid.NewGuid().ToString();
            var srtPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $"subs_{tempGuid}.srt");
            var audioListPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $"audio_list_{tempGuid}.txt");

            try
            {
                // 1. ADIM: Altyazı Dosyasını (.srt) Oluştur
                await GenerateSrtFileAsync(layout.CaptionTrack, srtPath);

                // 2. ADIM: Ses Listesi Oluştur (Concat Demuxer için)
                // Seslerin sırayla ve boşluksuz geldiğini varsayıyoruz (önceki aşamalarda ayarladık)
                await GenerateAudioListAsync(layout.AudioTrack, audioListPath);

                // 3. ADIM: FFmpeg Komutunu İnşa Et (The Beast!)
                var ffmpegArgs = BuildFFmpegCommand(layout, srtPath, audioListPath, outputPath);

                // 4. FFmpeg'i Çalıştır
                await RunFFmpegProcessAsync(ffmpegArgs, ct);

                return outputPath;
            }
            finally
            {
                // Temizlik: Geçici dosyaları sil
                if (File.Exists(srtPath)) File.Delete(srtPath);
                if (File.Exists(audioListPath)) File.Delete(audioListPath);
            }
        }

        // --- YARDIMCI METODLAR ---

        private async Task GenerateSrtFileAsync(List<CaptionEvent> captions, string path)
        {
            var sb = new StringBuilder();
            int index = 1;

            foreach (var cap in captions)
            {
                sb.AppendLine(index.ToString());

                // Zaman formatı: 00:00:05,123 --> 00:00:08,500
                var start = TimeSpan.FromSeconds(cap.Start).ToString(@"hh\:mm\:ss\,fff");
                var end = TimeSpan.FromSeconds(cap.End).ToString(@"hh\:mm\:ss\,fff");

                sb.AppendLine($"{start} --> {end}");
                sb.AppendLine(cap.Text); // İstersen burada HTML tagleri ile renk verebilirsin
                sb.AppendLine(); // Boş satır
                index++;
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        }

        private async Task GenerateAudioListAsync(List<AudioEvent> audios, string path)
        {
            var sb = new StringBuilder();
            foreach (var audio in audios.Where(a => a.Type == "voice").OrderBy(a => a.StartTime))
            {
                // FFmpeg concat formatı: file 'C:\path\to\file.mp3'
                // Windows path'leri için ters slash kaçışı gerekebilir ama safe yolu forward slash yapmaktır.
                var safePath = audio.FilePath.Replace("\\", "/");
                sb.AppendLine($"file '{safePath}'");
            }
            await File.WriteAllTextAsync(path, sb.ToString());
        }

        private string BuildFFmpegCommand(SceneLayoutStagePayload layout, string srtPath, string audioListPath, string outputPath)
        {
            // --- GİRDİLER ---
            var sb = new StringBuilder();
            var filter = new StringBuilder();

            // Girdi 0: Ses Dosyaları (Concat listesinden)
            sb.Append($"-f concat -safe 0 -i \"{audioListPath}\" ");

            // Girdi 1..N: Resim Dosyaları
            int imgInputIndex = 1; // 0 ses listesi olduğu için 1'den başlıyoruz
            foreach (var visual in layout.VisualTrack)
            {
                sb.Append($"-i \"{visual.ImagePath}\" ");
            }

            // --- FILTER COMPLEX (GÖRSEL EFEKTLER) ---
            sb.Append("-filter_complex \"");

            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                var v = layout.VisualTrack[i];
                var inputLabel = $"[{imgInputIndex + i}:v]";
                var zoomOutput = $"[v{i}]";

                // Zoom Efekti (Ken Burns)
                // z='if(eq(on,1),ZOOM_START,zoom+ZOOM_SPEED)'
                // 25 fps x 5 sn = 125 kare.
                // Zoom In: zoom+0.0015 | Zoom Out: zoom-0.0015

                string zoomExpr = v.EffectType == "zoom_in"
                    ? "min(zoom+0.0015,1.5)"
                    : "if(eq(on,1),1.5,max(1.0,zoom-0.0015))";

                // x ve y ile pan yapılabilir (basitlik için merkeze odaklıyoruz)
                string posExpr = "x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)'";

                // ÖNEMLİ: Resimler statik olduğu için 'zoompan' süresini (d) ayarlamalıyız.
                // d = süre * fps
                int frameCount = (int)(v.Duration * layout.Fps);

                filter.Append($"{inputLabel}zoompan=z='{zoomExpr}':{posExpr}:d={frameCount}:s={layout.Width}x{layout.Height}:fps={layout.Fps}{zoomOutput};");
            }

            // --- CONCAT (BİRLEŞTİRME) ---
            for (int i = 0; i < layout.VisualTrack.Count; i++)
            {
                filter.Append($"[v{i}]");
            }
            // n=SahneSayısı:v=1:a=0 (Sadece videoyu birleştiriyoruz, ses ayrı kanaldan geliyor)
            filter.Append($"concat=n={layout.VisualTrack.Count}:v=1:a=0[vbase];");

            // --- ALTYAZI GÖMME (BURNING) ---
            // Windows path'lerindeki ters slash ve iki nokta üst üste (:) FFmpeg filtresinde kaçış karakteri gerektirir.
            // En güvenlisi: Yolu forward slash yapıp, ':' karakterini kaçırmak.
            // Örn: D:/test.srt -> D\:/test.srt
            var safeSrtPath = srtPath.Replace("\\", "/").Replace(":", "\\:");

            // Subtitles filtresi ve Stil Ayarları
            // Font boyutu, rengi, hizalaması (Alignment=2 -> Bottom Center)
            filter.Append($"[vbase]subtitles='{safeSrtPath}':force_style='Alignment=2,OutlineColour=&H40000000,BorderStyle=3,Outline=1,Shadow=0,Fontsize=16,MarginV=30'[vfinal]\"");

            sb.Append(filter.ToString());

            // --- ÇIKTI AYARLARI ---
            // Map: [vfinal] (video) ve 0:a (ses)
            sb.Append($" -map \"[vfinal]\" -map 0:a ");

            // Encoding Ayarları
            sb.Append("-c:v libx264 -pix_fmt yuv420p -preset fast -crf 23 "); // Video: H.264
            sb.Append("-c:a aac -b:a 192k "); // Ses: AAC
            sb.Append("-shortest "); // En kısa olana göre kes (Video veya ses hangisi kısaysa)
            sb.Append($"-y \"{outputPath}\""); // Üzerine yaz

            return sb.ToString();
        }

        private async Task RunFFmpegProcessAsync(string arguments, CancellationToken ct)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // FFmpeg logları stderr'e yazar
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    // İstersen burada logları canlı parse edip ilerleme (percentage) hesaplayabilirsin
                }
            };

            process.Start();
            process.BeginErrorReadLine(); // Log okumayı başlat

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                // Hata durumunda son 20 satırı göster
                var logs = outputBuilder.ToString();
                var lastLogs = logs.Length > 2000 ? logs.Substring(logs.Length - 2000) : logs;
                throw new Exception($"FFmpeg Error (Exit Code {process.ExitCode}): {lastLogs}");
            }
        }
    }
}
