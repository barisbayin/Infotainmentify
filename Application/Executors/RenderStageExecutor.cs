using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Executors
{
    [StageExecutor(StageType.Render)]
    [StagePreset(typeof(RenderPreset))]
    public class RenderStageExecutor : BaseStageExecutor
    {
        private readonly IVideoRendererService _videoService;
        private readonly IUserDirectoryService _dirService;
        private readonly INotifierService _notifier;

        public RenderStageExecutor(
            IServiceProvider sp,
            IVideoRendererService videoService,
            IUserDirectoryService dirService,
            INotifierService notifier)
            : base(sp)
        {
            _videoService = videoService;
            _dirService = dirService;
            _notifier = notifier;
        }

        public override StageType StageType => StageType.Render;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            await logAsync("Final render hazırlanıyor. Kurgu planı FFmpeg'e gönderilecek.");

            // =================================================================
            // 1. LAYOUT'U BUL (Memory vs Database Stratejisi)
            // =================================================================

            // A) Önce Context'e (RAM) bak. (Normal akışta burası doludur)
            var layout = context.GetOutput<SceneLayoutStagePayload>(StageType.SceneLayout);

            // B) Eğer RAM boşsa (Retry/Re-Render senaryosu), Veritabanına bak.
            if (layout == null)
            {
                await logAsync(PipelineLiveLog.Warning("Kurgu planı bellekte bulunamadı. Retry/Re-render senaryosu için veritabanı geçmişi kontrol ediliyor."));

                // SceneLayout aşamasının kaydını bul
                var layoutExec = run.StageExecutions
                    .FirstOrDefault(x => x.StageConfig.StageType == StageType.SceneLayout);

                if (layoutExec != null && !string.IsNullOrEmpty(layoutExec.OutputJson))
                {
                    try
                    {
                        layout = JsonSerializer.Deserialize<SceneLayoutStagePayload>(layoutExec.OutputJson);
                        await logAsync(PipelineLiveLog.Success("Kurgu planı veritabanı geçmişinden geri yüklendi."));
                    }
                    catch (Exception ex)
                    {
                        await logAsync(PipelineLiveLog.Error($"Veritabanındaki kurgu planı okunamadı. Hata: {ex.Message}"));
                    }
                }
            }

            // Hala yoksa yapacak bir şey yok, patlat.
            if (layout == null || layout.VisualTrack == null || !layout.VisualTrack.Any())
                throw new InvalidOperationException("Kurgu planı (SceneLayout) ne hafızada ne de veritabanında bulunamadı!");

            // =================================================================
            // 2. PRESET OVERRIDE (YENİ AYARLARI UYGULA)
            // =================================================================
            // Re-Render yaparken kullanıcı yeni bir Preset seçmiş olabilir.
            // Bu durumda Layout içindeki eski stili, yeni Preset ile eziyoruz.

            if (presetObj is RenderPreset currentPreset)
            {
                await logAsync($"Render ayarları güncel preset üzerinden uygulanıyor: '{currentPreset.Name}'.");

                // Layout'un stilini tamamen yenisiyle değiştir
                layout.Style = new RenderStyleSettings
                {
                    BitrateKbps = currentPreset.BitrateKbps,
                    EncoderPreset = currentPreset.EncoderPreset,

                    // Alt Ayarlar
                    CaptionSettings = currentPreset.CaptionSettings,
                    AudioMixSettings = currentPreset.AudioMixSettings,
                    VisualEffectsSettings = currentPreset.VisualEffectsSettings,
                    BrandingSettings = currentPreset.BrandingSettings
                };

                // FFmpeg komutları için ana boyutları da güncelle
                layout.Width = currentPreset.OutputWidth;
                layout.Height = currentPreset.OutputHeight;
                layout.Fps = currentPreset.Fps;
            }
            else
            {
                await logAsync("Yeni render preset gelmedi. Kurgu planındaki mevcut stil ayarları kullanılacak.");
            }

            // =================================================================
            // 3. HAZIRLIK VE RENDER
            // =================================================================

            // Çıktı Klasörünü Hazırla
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "video");
            var fileName = $"final_video_{run.Id}_{DateTime.Now.Ticks}.mp4";
            var outputPath = Path.Combine(outputDir, fileName);

            await logAsync($"Render çıktı dosyası hazırlandı: {fileName}.");
            await logAsync($"Render girdisi: {layout.VisualTrack.Count} görsel vuruş, toplam süre: {layout.TotalDuration:F1} sn, çözünürlük: {layout.Width}x{layout.Height}, FPS: {layout.Fps}.");

            // Dil Kodunu Belirle (DB'den veya Default)
            string langCode = !string.IsNullOrEmpty(run.Language) ? run.Language : "en-US";
            await logAsync($"Render dil modu: {langCode}.");

            await logAsync("FFmpeg render işlemi başladı. Bu aşama uzun sürebilir.");
            await SendRenderProgressSafeAsync(run.Id, new RenderProgressUpdate
            {
                Label = "Render hazirlaniyor",
                Percent = 0,
                CurrentSeconds = 0,
                TotalSeconds = layout.TotalDuration
            });

            try
            {
                // 🔥 FFmpeg Servisini Çağır
                var finalPath = await _videoService.RenderVideoAsync(
                    layout,
                    outputPath,
                    langCode,
                    ct,
                    logAsync,
                    progress => SendRenderProgressSafeAsync(run.Id, progress));

                var fileInfo = new FileInfo(finalPath);
                double sizeMb = fileInfo.Length / (1024.0 * 1024.0);
                var audioQa = layout.Style.AudioMixSettings.EnableFinalAudioQa
                    ? await AnalyzeAudioQaAsync(finalPath, layout.TotalDuration, logAsync, ct)
                    : null;

                await logAsync(PipelineLiveLog.Success($"Render başarıyla tamamlandı. Dosya boyutu: {sizeMb:F2} MB."));
                await SendRenderProgressSafeAsync(run.Id, new RenderProgressUpdate
                {
                    Label = "Render tamamlandi",
                    Percent = 100,
                    CurrentSeconds = layout.TotalDuration,
                    TotalSeconds = layout.TotalDuration,
                    IsCompleted = true
                });

                // URL Oluşturma
                // UserDirectoryService'e 'GetPublicUrl' metodunu eklediysen onu kullan:
                // var webUrl = _dirService.GetPublicUrl(finalPath);

                // Eklememiş olma ihtimaline karşı manuel ama güvenli yöntem:
                var webUrl = $"/UserFiles/User_{run.AppUserId}/runs/Run_{run.Id}/video/{fileName}";

                // 4. Sonuç Dön
                return new RenderStagePayload
                {
                    SceneLayoutId = 0,
                    VideoFilePath = finalPath,
                    VideoUrl = webUrl,
                    Width = layout.Width,
                    Height = layout.Height,
                    Fps = layout.Fps,
                    AspectRatio = BuildAspectRatio(layout.Width, layout.Height),
                    FileSizeMb = sizeMb,
                    Duration = layout.TotalDuration,
                    AudioQa = audioQa
                };
            }
            catch (Exception ex)
            {
                // Hata durumunda loga bas ve fırlat
                await logAsync(PipelineLiveLog.Error($"FFmpeg render hatası: {ex.Message}"));
                throw;
            }
        }

        private async Task SendRenderProgressSafeAsync(int runId, RenderProgressUpdate progress)
        {
            try
            {
                progress.RunId = runId;
                await _notifier.SendRenderProgressAsync(runId, progress);
            }
            catch
            {
                // Progress event'i render akışını bozmasın.
            }
        }

        private static string BuildAspectRatio(int width, int height)
        {
            if (width <= 0 || height <= 0) return "unknown";
            var gcd = Gcd(width, height);
            return $"{width / gcd}:{height / gcd}";
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                var t = b;
                b = a % b;
                a = t;
            }

            return Math.Abs(a);
        }

        private static async Task<RenderAudioQaReport?> AnalyzeAudioQaAsync(
            string videoPath,
            double expectedDuration,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            try
            {
                await logAsync("Render sonrası audio QA analizi başladı. Peak, ortalama ses ve sessizlik taranıyor.");

                var volumeLog = await RunProcessCaptureAsync(
                    "ffmpeg",
                    $"-hide_banner -i \"{videoPath}\" -af volumedetect -f null -",
                    ct);

                var silenceLog = await RunProcessCaptureAsync(
                    "ffmpeg",
                    $"-hide_banner -i \"{videoPath}\" -af silencedetect=noise=-45dB:d=2 -f null -",
                    ct);

                var report = new RenderAudioQaReport
                {
                    DurationSec = expectedDuration,
                    MeanVolumeDb = ParseDb(volumeLog, @"mean_volume:\s*(-?\d+(?:\.\d+)?)\s*dB"),
                    MaxVolumeDb = ParseDb(volumeLog, @"max_volume:\s*(-?\d+(?:\.\d+)?)\s*dB")
                };

                foreach (Match match in Regex.Matches(silenceLog, @"silence_duration:\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase))
                {
                    if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
                    {
                        report.SilenceDurationSec += duration;
                        report.SilenceSegmentCount++;
                    }
                }

                if (expectedDuration > 0)
                    report.SilenceRatio = Math.Round(report.SilenceDurationSec / expectedDuration, 4);

                if (report.MeanVolumeDb < -24)
                    report.Warnings.Add("Ortalama ses seviyesi düşük görünüyor.");
                if (report.MaxVolumeDb > -0.3)
                    report.Warnings.Add("Peak seviyesi clipping riskine yakın.");
                if (report.SilenceRatio > 0.12)
                    report.Warnings.Add("Videoda beklenenden fazla sessizlik algılandı.");

                report.Status = report.Warnings.Count == 0 ? "Ready" : "Review";

                var warningText = report.Warnings.Count == 0
                    ? "uyarı yok"
                    : string.Join(" ", report.Warnings);

                await logAsync(
                    $"Audio QA tamamlandı. Ortalama: {report.MeanVolumeDb:F1} dB, peak: {report.MaxVolumeDb:F1} dB, sessizlik: {report.SilenceDurationSec:F1} sn ({report.SilenceRatio:P1}), durum: {report.Status}. {warningText}");

                return report;
            }
            catch (Exception ex)
            {
                await logAsync(PipelineLiveLog.Warning($"Audio QA analizi tamamlanamadı. Render geçerli kabul edildi. Hata: {PipelineLiveLog.Shorten(ex.Message, 220)}"));
                return null;
            }
        }

        private static double ParseDb(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) return 0;

            return double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static async Task<string> RunProcessCaptureAsync(string fileName, string arguments, CancellationToken ct)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
                throw new InvalidOperationException(output.ToString());

            return output.ToString();
        }
    }
}
