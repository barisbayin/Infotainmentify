using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.Image)]
    [StagePreset(typeof(ImagePreset))]
    public class ImageStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IUserDirectoryService _dirService; // Dosya kaydetmek için

        public ImageStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IUserDirectoryService dirService)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _dirService = dirService;
        }

        public override StageType StageType => StageType.Image;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            var preset = (ImagePreset)presetObj!;
            exec.AddLog($"Starting Image Generation with preset: {preset.Name} ({preset.ModelName})");

            // 1. Önceki Adımdan (Script) Veriyi Çek
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            if (scriptData == null || scriptData.Scenes == null || !scriptData.Scenes.Any())
                throw new InvalidOperationException("Script verisi bulunamadı veya sahneler boş.");

            exec.AddLog($"Found {scriptData.Scenes.Count} scenes to visualize.");

            // 2. AI İstemcisi
            var aiClient = await _aiFactory.ResolveImageClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 3. Kayıt Klasörü Hazırla
            // Örn: /users/1/runs/105/images/
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "images");

            var results = new List<SceneImageItem>();

            // 4. Döngü (Sahneleri işle)
            // Not: DALL-E rate limit'e takılmamak için 'Semaphore' ile eşzamanlılığı sınırlayabilirsin.
            // Şimdilik basit foreach ile gidelim (Sıralı).

            int successCount = 0;

            foreach (var scene in scriptData.Scenes)
            {
                if (ct.IsCancellationRequested) break;

                exec.AddLog($"Generating image for Scene {scene.SceneNumber}...");

                // Prompt Hazırla
                // Şablon: "{SceneDescription}, style of {ArtStyle}"
                var finalPrompt = preset.PromptTemplate
                    .Replace("{SceneDescription}", scene.VisualPrompt)
                    .Replace("{ArtStyle}", preset.ArtStyle ?? "cinematic")
                    .Trim();

                try
                {
                    // AI Çağrısı (Byte Array döner)
                    var imageBytes = await aiClient.GenerateImageAsync(
                        prompt: finalPrompt,
                        negativePrompt: preset.NegativePrompt,
                        size: preset.Size, // "1024x1792"
                        style: preset.ArtStyle,
                        model: preset.ModelName,
                        ct: ct
                    );

                    // Dosyayı Kaydet
                    var fileName = $"scene_{scene.SceneNumber:00}_{Guid.NewGuid().ToString()[..6]}.png";
                    var fullPath = Path.Combine(outputDir, fileName);

                    await File.WriteAllBytesAsync(fullPath, imageBytes, ct);

                    results.Add(new SceneImageItem
                    {
                        SceneNumber = scene.SceneNumber,
                        ImagePath = fullPath,
                        PromptUsed = finalPrompt
                    });

                    successCount++;
                    exec.AddLog($"Scene {scene.SceneNumber} ready: {fileName}");
                }
                catch (Exception ex)
                {
                    exec.AddLog($"ERROR Scene {scene.SceneNumber}: {ex.Message}");
                    // Hata olsa bile devam edelim mi? 
                    // Şimdilik devam ediyoruz, eksik resimle render yapılmaz ama logda görünsün.
                }

                // API'yi boğmamak için minik bekleme
                await Task.Delay(1000, ct);
            }

            if (successCount == 0)
                throw new Exception("Hiçbir görsel üretilemedi. Lütfen API ayarlarını veya kotanızı kontrol edin.");

            // 5. Sonuç Dön
            return new ImageStagePayload
            {
                ScriptId = scriptData.ScriptId,
                SceneImages = results
            };
        }
    }

}
