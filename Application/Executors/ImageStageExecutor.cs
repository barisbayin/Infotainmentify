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
        private readonly IUserDirectoryService _dirService;

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

        // 🔥 DÜZELTME 1: Access Modifier 'protected override' olmalı (Base sınıf öyle istiyor)
        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync, // 🔥 Bu fonksiyonu kullanacağız
            CancellationToken ct)
        {
            var preset = (ImagePreset)presetObj!;

            // 🔥 DÜZELTME 2: exec.AddLog yerine logAsync kullanıyoruz
            await logAsync($"🎨 Starting Image Generation with preset: {preset.Name} ({preset.ModelName})");

            // 1. Önceki Adımdan (Script) Veriyi Çek
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            if (scriptData == null || scriptData.Scenes == null || !scriptData.Scenes.Any())
                throw new InvalidOperationException("Script verisi bulunamadı veya sahneler boş.");

            await logAsync($"Found {scriptData.Scenes.Count} scenes to visualize.");

            // 2. AI İstemcisi
            var aiClient = await _aiFactory.ResolveImageClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 3. Kayıt Klasörü Hazırla
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "images");

            var results = new List<SceneImageItem>();
            int successCount = 0;

            // 4. Döngü (Sahneleri işle)
            foreach (var scene in scriptData.Scenes)
            {
                if (ct.IsCancellationRequested) break;

                await logAsync($"🖌️ Generating image for Scene {scene.SceneNumber}...");

                // Prompt Hazırla
                var finalPrompt = preset.PromptTemplate
                    .Replace("{SceneDescription}", scene.VisualPrompt)
                    .Replace("{ArtStyle}", preset.ArtStyle ?? "cinematic")
                    .Trim();

                try
                {
                    // AI Çağrısı
                    var imageBytes = await aiClient.GenerateImageAsync(
                        prompt: finalPrompt,
                        negativePrompt: preset.NegativePrompt,
                        size: preset.Size,
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
                    // Başarılı log
                    await logAsync($"✅ Scene {scene.SceneNumber} ready: {fileName}");
                }
                catch (Exception ex)
                {
                    var errorMsg = $"❌ Scene {scene.SceneNumber} generation failed. Error: {ex.Message}";

                    // Güvenlik filtresi uyarısı
                    if (ex.Message.Contains("safety") || ex.Message.Contains("content") || ex.Message.Contains("NO_IMAGE"))
                    {
                        errorMsg += " [OLASI SEBEP: Prompt içindeki yasaklı kelimeler (die, blood, shave vb.)]";
                    }

                    // Hata logunu canlıya bas
                    await logAsync(errorMsg);

                    // Not: Burası catch bloğu olduğu için BaseExecutor zaten bu exception'ı yakalamayacak 
                    // (çünkü biz burada yuttuk ve logladık). Eğer sahneyi atlayıp devam etmek istiyorsak
                    // 'throw' demeden devam ediyoruz. (Fallback mantığı için)
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
