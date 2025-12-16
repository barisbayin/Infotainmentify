using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using System.Text.Json;

namespace Application.Executors
{
    [StageExecutor(StageType.Render)]
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
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            await logAsync("🎬 Starting Video Rendering Process...");

            // =================================================================
            // 1. LAYOUT'U BUL (Memory vs Database Stratejisi)
            // =================================================================

            // A) Önce Context'e (RAM) bak. (Normal akışta burası doludur)
            var layout = context.GetOutput<SceneLayoutStagePayload>(StageType.SceneLayout);

            // B) Eğer RAM boşsa (Retry/Re-Render senaryosu), Veritabanına bak.
            if (layout == null)
            {
                await logAsync("⚠️ Context is empty (Retry/Re-Render detected). Fetching layout from Database history...");

                // SceneLayout aşamasının kaydını bul
                var layoutExec = run.StageExecutions
                    .FirstOrDefault(x => x.StageConfig.StageType == StageType.SceneLayout);

                if (layoutExec != null && !string.IsNullOrEmpty(layoutExec.OutputJson))
                {
                    try
                    {
                        layout = JsonSerializer.Deserialize<SceneLayoutStagePayload>(layoutExec.OutputJson);
                        await logAsync("✅ Layout successfully restored from Database.");
                    }
                    catch (Exception ex)
                    {
                        await logAsync($"❌ Failed to deserialize layout from DB: {ex.Message}");
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
                await logAsync($"⚙️ Applying updated render settings from Preset: '{currentPreset.Name}'");

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
                await logAsync("ℹ️ No new preset provided. Using cached styles from layout.");
            }

            // =================================================================
            // 3. HAZIRLIK VE RENDER
            // =================================================================

            // Çıktı Klasörünü Hazırla
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "video");
            var fileName = $"final_video_{run.Id}_{DateTime.Now.Ticks}.mp4";
            var outputPath = Path.Combine(outputDir, fileName);

            await logAsync($"Target Output: {fileName}");
            await logAsync($"Processing {layout.VisualTrack.Count} scenes. Total Duration: {layout.TotalDuration:F1}s");

            // Dil Kodunu Belirle (DB'den veya Default)
            string langCode = !string.IsNullOrEmpty(run.Language) ? run.Language : "en-US";
            await logAsync($"Language Mode: {langCode}");

            await logAsync("⏳ FFmpeg engine initialized. Rendering started (this may take a while)...");

            try
            {
                // 🔥 FFmpeg Servisini Çağır
                var finalPath = await _videoService.RenderVideoAsync(layout, outputPath, langCode, ct);

                var fileInfo = new FileInfo(finalPath);
                double sizeMb = fileInfo.Length / (1024.0 * 1024.0);

                await logAsync($"✅ Render Completed Successfully! Size: {sizeMb:F2} MB");

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
                    FileSizeMb = sizeMb,
                    Duration = layout.TotalDuration
                };
            }
            catch (Exception ex)
            {
                // Hata durumunda loga bas ve fırlat
                await logAsync($"❌ FFMPEG FATAL ERROR: {ex.Message}");
                throw;
            }
        }
    }
}
