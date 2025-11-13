using Application.Abstractions;
using System.Diagnostics;

namespace Application.Services
{
    /// <summary>
    /// FFmpeg tabanlı video render yardımcı servisi.
    /// Görsel + ses birleşimi, video birleştirme ve metadata çıkarımı yapar.
    /// </summary>
    public class FFmpegService : IFFmpegService
    {
        private readonly INotifierService _notifier;

        public FFmpegService(INotifierService notifier)
        {
            _notifier = notifier;
        }

        private static string FindFfmpegPath()
        {
            var possible = new[]
            {
                "ffmpeg",
                Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe")
            };
            return possible.FirstOrDefault(File.Exists)
                ?? throw new InvalidOperationException("FFmpeg bulunamadı. PATH veya uygulama dizinine ekleyin.");
        }

        /// <summary>
        /// Görsel + ses kombinasyonundan tek bir sahne videosu üretir.
        /// </summary>
        public async Task GenerateSceneVideoAsync(
            string imagePath,
            string audioPath,
            string outputPath,
            CancellationToken ct = default)
        {
            var ffmpeg = FindFfmpegPath();

            var args =
                $"-y -loop 1 -i \"{imagePath}\" -i \"{audioPath}\" " +
                "-c:v libx264 -c:a aac -b:a 192k -shortest " +
                "-pix_fmt yuv420p -vf scale=1080:1920,fps=30 " +
                $"\"{outputPath}\"";

            var psi = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("FFmpeg başlatılamadı.");

            var err = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"FFmpeg sahne render hatası: {err}");
        }

        /// <summary>
        /// Birden fazla sahneyi tek bir final videoda birleştirir.
        /// </summary>
        public async Task ConcatVideosAsync(
            IReadOnlyList<string> sceneFiles,
            string outputPath,
            CancellationToken ct = default)
        {
            var ffmpeg = FindFfmpegPath();

            if (sceneFiles == null || sceneFiles.Count == 0)
                throw new InvalidOperationException("Birleştirilecek sahne videosu bulunamadı.");

            // Geçici list dosyası
            var listFile = Path.GetTempFileName();
            await File.WriteAllLinesAsync(listFile, sceneFiles.Select(f => $"file '{f.Replace("\\", "/")}'"), ct);

            var args = $"-f concat -safe 0 -i \"{listFile}\" -c copy \"{outputPath}\"";

            var psi = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("FFmpeg başlatılamadı.");

            var err = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            File.Delete(listFile);

            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"FFmpeg birleştirme hatası: {err}");
        }

        /// <summary>
        /// Render edilen videonun süresini (saniye cinsinden) döner.
        /// </summary>
        public async Task<double?> GetVideoDurationAsync(string filePath, CancellationToken ct = default)
        {
            var ffmpeg = FindFfmpegPath();
            var args = $"-i \"{filePath}\" 2>&1";

            var psi = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("FFmpeg başlatılamadı.");

            var output = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            var match = System.Text.RegularExpressions.Regex.Match(output, @"Duration:\s*(\d+):(\d+):(\d+\.\d+)");
            if (match.Success)
            {
                double h = double.Parse(match.Groups[1].Value);
                double m = double.Parse(match.Groups[2].Value);
                double s = double.Parse(match.Groups[3].Value);
                return h * 3600 + m * 60 + s;
            }

            return null;
        }

        /// <summary>
        /// Thumbnail üretimi (isteğe bağlı): final videodan ilk kareyi alır.
        /// </summary>
        public async Task<string> GenerateThumbnailAsync(string videoPath, string outputDir, CancellationToken ct = default)
        {
            var ffmpeg = FindFfmpegPath();
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, "thumbnail.jpg");
            var args = $"-y -i \"{videoPath}\" -ss 00:00:01.000 -vframes 1 \"{outputPath}\"";

            var psi = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("FFmpeg başlatılamadı.");

            await proc.WaitForExitAsync(ct);
            if (proc.ExitCode != 0)
            {
                var err = await proc.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"FFmpeg thumbnail hatası: {err}");
            }

            return outputPath.Replace("\\", "/");
        }
    }
}
