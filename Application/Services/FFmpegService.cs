using Application.Abstractions;
using Application.Contracts.Script;
using Core.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Application.Services
{
    /// <summary>
    /// Profesyonel Shorts FFmpeg pipeline servisi.
    /// - Her sahne kendi sesiyle görüntüye işlenir
    /// - Sahne süresi audio süresi kadar olur
    /// - Ken Burns, fade, subtitles destekli
    /// - Sahneler kaymasız concat edilir
    /// </summary>
    public class FFmpegService : IFFmpegService
    {
        private readonly INotifierService _notifier;

        public FFmpegService(INotifierService notifier)
        {
            _notifier = notifier;
        }

        // ---------------------------------------------------
        // GLOBAL PRESETLER (Shorts için optimize)
        // ---------------------------------------------------
        private const int W = 720;
        private const int H = 1280;
        private const int FPS = 24;
        private const int GOP = FPS * 2;    // 2 saniye
        private const int CRF = 24;
        private const int AUDIO_K = 64;
        private const int MAXRATE_K = 1500;
        private const int BUFSIZE_K = 3000;
        private const string X264_PRESET = "slow";
        private const string X264_TUNE = "stillimage";

        // ---------------------------------------------------
        // FFmpeg bulucu
        // ---------------------------------------------------
        private static string FindFfmpeg()
        {
            var list = new[]
            {
                "ffmpeg",
                Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe")
            };
            return list.FirstOrDefault(File.Exists)
                ?? throw new InvalidOperationException("FFmpeg bulunamadı.");
        }

        private static string FindFfprobe()
        {
            var list = new[]
            {
                "ffprobe",
                Path.Combine(AppContext.BaseDirectory, "ffprobe.exe")
            };
            return list.FirstOrDefault(File.Exists)
                ?? throw new InvalidOperationException("ffprobe bulunamadı.");
        }

        // ---------------------------------------------------
        // TEMEL RUNNER
        // ---------------------------------------------------
        private async Task RunAsync(string args, CancellationToken ct)
        {
            var ffmpeg = FindFfmpeg();
            var psi = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi) ?? throw new("FFmpeg başlatılamadı.");
            var err = await p.StandardError.ReadToEndAsync(ct);
            var outp = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            if (p.ExitCode != 0)
                throw new Exception($"FFmpeg hata ({p.ExitCode}):\n{err}\n{outp}");
        }

        private async Task RunAsync(IEnumerable<string> args, CancellationToken ct)
        {
            var ffmpeg = FindFfmpeg();

            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            try
            {
                using var p = Process.Start(psi)
                    ?? throw new InvalidOperationException("FFmpeg başlatılamadı.");

                // Paralel okuma
                var errTask = p.StandardError.ReadToEndAsync(ct);
                var outTask = p.StandardOutput.ReadToEndAsync(ct);

                await p.WaitForExitAsync(ct);

                var err = await errTask;
                var outp = await outTask;

                if (p.ExitCode != 0)
                {
                    throw new Exception(
                        $"FFmpeg ExitCode={p.ExitCode}\n\n" +
                        $"--- STDERR ---\n{err}\n\n" +
                        $"--- STDOUT ---\n{outp}"
                    );
                }
            }
            catch (OperationCanceledException)
            {
                throw; // pipeline iptalinde doğal olarak yukarı fırlatılır
            }
            catch (Exception ex)
            {
                // FFmpeg args'ı da gösterelim → debugging cennet gibi olur
                var argText = string.Join(" ", args);

                throw new Exception(
                    $"FFmpeg çalıştırma hatası:\n" +
                    $"File: {ffmpeg}\n" +
                    $"Args: {argText}\n\n" +
                    $"Message: {ex.Message}",
                    ex
                );
            }
        }


        // ------------------------------------------------------------
        // SES SÜRESİ ÖLÇÜMÜ (wav/mp3 farketmez)
        // ------------------------------------------------------------
        public async Task<double?> GetAudioDurationAsync(string path, CancellationToken ct = default)
        {
            var ffprobe = FindFfprobe();

            var psi = new ProcessStartInfo(ffprobe)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add("error");
            psi.ArgumentList.Add("-select_streams");
            psi.ArgumentList.Add("a:0");
            psi.ArgumentList.Add("-show_entries");
            psi.ArgumentList.Add("stream=duration");
            psi.ArgumentList.Add("-of");
            psi.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
            psi.ArgumentList.Add(path);

            using var p = Process.Start(psi)!;

            var text = await p.StandardOutput.ReadToEndAsync();
            await p.WaitForExitAsync(ct);

            if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            return null;
        }

        // ------------------------------------------------------------
        // SAHNE ÜRETİMİ (image + wav -> mp4)
        // ------------------------------------------------------------
        public async Task GenerateSceneVideoAsync(
     string image,
     string audio,
     string output,
     string? assPath = null,
     CancellationToken ct = default)
        {
            var audioDur = await GetAudioDurationAsync(audio, ct) ?? 1.0;
            var sec = audioDur + 0.15;
            int frames = (int)Math.Ceiling(sec * FPS);

            var imgAbs = Path.GetFullPath(image);
            var audAbs = Path.GetFullPath(audio);
            var outAbs = Path.GetFullPath(output);

            Directory.CreateDirectory(Path.GetDirectoryName(outAbs)!);

            // ---- FİLTLER ----
            string baseFilter =
                $"scale={W}:{H}:force_original_aspect_ratio=increase," +
                $"crop={W}:{H}," +
                $"zoompan=z='min(zoom+0.00025,1.04)':d={frames}:s={W}x{H}:fps={FPS}," +
                $"fade=t=in:st=0:n=6," +
                $"fade=t=out:st={(sec - 0.25).ToString(CultureInfo.InvariantCulture)}:d=0.25";

            string filter;

            if (!string.IsNullOrWhiteSpace(assPath))
            {
                // ASS BOMSUZ OLMALI
                var assAbs = Path.GetFullPath(assPath);
                var escaped = EscapeAssPath(assAbs);

                filter =
                    $"[0:v]{baseFilter}[vz];" +
                    $"[vz]ass='{escaped}',format=yuv420p[vout]";
            }
            else
            {
                filter =
                    $"[0:v]{baseFilter},format=yuv420p[vout]";
            }

            var args = new List<string>
    {
        "-y",
        "-loop", "1", "-i", imgAbs,
        "-i", audAbs,
        "-filter_complex", filter,
        "-map", "[vout]",
        "-map", "1:a",
        "-c:v", "libx264",
        "-preset", X264_PRESET,
        "-tune", X264_TUNE,
        "-crf", CRF.ToString(),
        "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        "-b:a", $"{AUDIO_K}k",
        "-shortest",
        "-movflags", "+faststart",
        outAbs
    };

            await RunAsync(args, ct);
        }


        private static string EscapeAssPath(string path)
        {
            return path
                .Replace("\\", "/")
                .Replace(":", "\\:")
                .Replace("'", "\\'");
        }


        // ------------------------------------------------------------
        // SAHNELERİ CONCAT ET (zero-gap)
        // ------------------------------------------------------------
        public async Task ConcatVideosAsync(
            IReadOnlyList<string> scenes,
            string output,
            CancellationToken ct = default)
        {
            if (scenes == null || scenes.Count == 0)
                throw new Exception("Concat için sahne yok.");

            var workDir = Path.GetTempPath();
            var listFile = Path.Combine(workDir, $"concat_{Guid.NewGuid()}.txt");

            using (var sw = new StreamWriter(listFile, false, new UTF8Encoding(false)))
            {
                foreach (var f in scenes)
                {
                    var p = Path.GetFullPath(f)
                        .Replace("\\", "/")
                        .Replace("'", "'\\''");
                    sw.WriteLine($"file '{p}'");
                }
            }

            var outAbs = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(outAbs)!);

            var args = new List<string>
            {
                "-y",
                "-f","concat","-safe","0",
                "-i", listFile,
                "-c:v","libx264","-preset",X264_PRESET,"-tune",X264_TUNE,
                "-crf", CRF.ToString(),
                "-pix_fmt","yuv420p",
                "-c:a","aac","-b:a",$"{AUDIO_K}k",
                "-movflags","+faststart",
                outAbs
            };

            await RunAsync(args, ct);

            File.Delete(listFile);
        }

        // ------------------------------------------------------------
        // VİDEO SÜRESİ ÖLÇÜMÜ
        // ------------------------------------------------------------
        public async Task<double?> GetVideoDurationAsync(string filePath, CancellationToken ct = default)
        {
            var ffprobe = FindFfprobe();

            var psi = new ProcessStartInfo(ffprobe)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            psi.ArgumentList.Add("-v");
            psi.ArgumentList.Add("error");
            psi.ArgumentList.Add("-show_entries");
            psi.ArgumentList.Add("format=duration");
            psi.ArgumentList.Add("-of");
            psi.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
            psi.ArgumentList.Add(filePath);

            using var p = Process.Start(psi)!;
            var text = await p.StandardOutput.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            return null;
        }


        // ------------------------------------------------------------
        // THUMBNAIL
        // ------------------------------------------------------------
        public async Task<string> GenerateThumbnailAsync(
            string video,
            string outDir,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(outDir);
            var outJpg = Path.Combine(outDir, "thumbnail.jpg");

            var args =
                $"-y -ss 1 -i \"{video}\" -frames:v 1 -q:v 2 \"{outJpg}\"";

            await RunAsync(args, ct);

            return outJpg;
        }



        public async Task RenderTimelineAsync(
            List<string> visuals,
            string audioPath,
            string assPath,
            AutoVideoRenderProfile profile,
            string outputFile,
            CancellationToken ct = default)
        {
            if (visuals == null || visuals.Count == 0)
                throw new Exception("RenderTimelineAsync: visuals boş.");

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            int visualCount = visuals.Count;

            // TIMELINE SLOT HESABI
            double audioDuration = await GetAudioDurationAsync(audioPath, ct) ?? 1.0;
            double slotSeconds = profile.TimelineMode == "even"
                ? audioDuration / visualCount
                : audioDuration / visualCount; // (weighted ileride gelecek)

            // ---------------------------------------------------------
            // 1) FFmpeg INPUTS
            // ---------------------------------------------------------
            var args = new List<string> { "-y" };

            foreach (var v in visuals)
                args.AddRange(new[] { "-i", v });

            args.AddRange(new[] { "-i", audioPath });

            int audioIndex = visualCount;

            // ---------------------------------------------------------
            // 2) FILTER COMPLEX
            // ---------------------------------------------------------
            var fc = new StringBuilder();

            // 2A — SCENE RENDER CHAINS
            for (int i = 0; i < visualCount; i++)
                fc.AppendLine(SceneFilter(i, slotSeconds, profile));

            // 2B — CONCAT
            fc.AppendLine(BuildConcat(visualCount));

            // 2C — SUBTITLES OVERLAY
            string escapedAss = EscapeAss(assPath);
            fc.AppendLine($"[vconcat]ass='{escapedAss}'[vsub];");

            // 2D — AUDIO MIX
            fc.AppendLine(BuildAudioMix(audioIndex, profile));

            // ---------------------------------------------------------
            // 3) OUTPUT MAP
            // ---------------------------------------------------------
            args.AddRange(new[]
            {
        "-filter_complex", fc.ToString(),
        "-map", "[vsub]",
        "-map", "[amix]",
        "-c:v", "libx264",
        "-preset", "slow",
        "-crf", "22",
        "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        "-b:a", "128k",
        "-movflags", "+faststart",
        outputFile
    });

            await RunAsync(args, ct);
        }


        private string SceneFilter(int i, double slot, AutoVideoRenderProfile p)
        {
            string F(double v) => v.ToString(CultureInfo.InvariantCulture);

            var res = p.Resolution.Split('x');
            int targetW = int.Parse(res[0]); // 1080
            int targetH = int.Parse(res[1]); // 1920

            int fps = p.Fps;
            double fadeIn = 0.25;
            double fadeOut = 0.25;

            double fadeOutStart = slot - fadeOut;
            if (fadeOutStart < 0) fadeOutStart = 0;

            double zoomSpeed = p.ZoomSpeed;
            double zoomMax = p.ZoomMax;
            double panX = p.PanX;
            double panY = p.PanY;

            return
                $"[{i}:v]" +
                // 1) Yüksekliği 1920’ye getir, genişlik otomatik büyüsün
                $"scale=-1:{targetH}:force_original_aspect_ratio=decrease," +
                // 2) Genişlik 1080 değilse kırp
                $"crop={targetW}:{targetH}," +
                // 3) Zoom + Pan
                $"zoompan=z='min(zoom+{F(zoomSpeed)},{F(zoomMax)})':" +
                $"x='{F(panX)}':y='{F(panY)}':" +
                $"d=({fps}*{F(slot)}):s={targetW}x{targetH}:fps={fps}," +
                // 4) Fade'ler
                $"fade=t=in:st=0:d={F(fadeIn)}," +
                $"fade=t=out:st={F(fadeOutStart)}:d={F(fadeOut)}" +
                $"[v{i}];";
        }




        private string BuildConcat(int count)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < count; i++)
                sb.Append($"[v{i}]");

            sb.Append($"concat=n={count}:v=1:a=0[vconcat];");

            return sb.ToString();
        }

        private string BuildAudioMix(int audioIndex, AutoVideoRenderProfile p)
        {
            string F(double v) => v.ToString(CultureInfo.InvariantCulture);

            double voiceVol = p.VoiceVolume / 100.0;
            double bgmVol = p.BgmVolume / 100.0;
            double duck = p.DuckingStrength / 100.0;

            return
                $"[{audioIndex}:a]volume={F(voiceVol)}[a_voice];" +
                $"[{audioIndex}:a]volume={F(bgmVol)}[a_bgm];" +
                $"[a_voice][a_bgm]amix=inputs=2:weights='{F(1)} {F(1 - duck)}'[amix];";
        }

        private string EscapeAss(string path)
        {
            return path
                .Replace("\\", "/")
                .Replace(":", "\\:")
                .Replace("'", "\\'");
        }

    }
}
