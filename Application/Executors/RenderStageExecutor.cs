using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.Render)]
    // Bu aşama da RenderPreset kullanır (Bitrate, Codec vb. ayarlar için)
    [StagePreset(typeof(RenderPreset))]
    public class RenderStageExecutor : BaseStageExecutor
    {
        private readonly IVideoRendererService _videoService;
        private readonly IUserDirectoryService _dirService;

        public RenderStageExecutor(
            IServiceProvider sp,
            IVideoRendererService videoService,
            IUserDirectoryService dirService)
            : base(sp)
        {
            _videoService = videoService;
            _dirService = dirService;
        }

        public override StageType StageType => StageType.Render;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            exec.AddLog("Starting Video Rendering Process...");

            // 1. SceneLayout (Planı) Çek
            var layout = context.GetOutput<SceneLayoutStagePayload>(StageType.SceneLayout);
            if (layout == null || !layout.VisualTrack.Any())
                throw new InvalidOperationException("Kurgu planı (SceneLayout) bulunamadı veya boş.");

            // 2. Çıktı Klasörünü Hazırla
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "video");
            var fileName = $"final_video_{run.Id}_{DateTime.Now.Ticks}.mp4";
            var outputPath = Path.Combine(outputDir, fileName);

            exec.AddLog($"Target Output: {fileName}");
            exec.AddLog($"Processing {layout.VisualTrack.Count} scenes...");

            // 3. RENDER BAŞLASIN! (FFmpeg)
            // Bu işlem uzun sürer (CPU/GPU)
            try
            {
                var finalPath = await _videoService.RenderVideoAsync(layout, outputPath, ct);

                var fileInfo = new FileInfo(finalPath);
                double sizeMb = fileInfo.Length / (1024.0 * 1024.0);

                exec.AddLog($"Render Completed! Size: {sizeMb:F2} MB");

                // 4. Sonuç Dön
                return new RenderStagePayload
                {
                    SceneLayoutId = 0, // Opsiyonel
                    VideoFilePath = finalPath,
                    // URL oluşturma işini frontend'e veya helper'a bırakabiliriz ama burada basitçe verelim
                    VideoUrl = $"/User_/{run.AppUserId}/runs/Run_{run.Id}/video/{fileName}",
                    FileSizeMb = sizeMb,
                    Duration = layout.TotalDuration
                };
            }
            catch (Exception ex)
            {
                exec.AddLog($"FFMPEG FATAL ERROR: {ex.Message}");
                throw; // Hatayı yukarı fırlat ki süreç Failed olsun
            }
        }
    }
}
