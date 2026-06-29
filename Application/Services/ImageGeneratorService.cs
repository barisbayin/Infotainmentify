using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Executors;
using Application.Services.Interfaces;
using Core.Entity.Presets;

namespace Application.Services
{
    public class ImageGeneratorService : IImageGeneratorService
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IUserDirectoryService _dirService;

        public ImageGeneratorService(IAiGeneratorFactory aiFactory, IUserDirectoryService dirService)
        {
            _aiFactory = aiFactory;
            _dirService = dirService;
        }

        public async Task<string> GenerateAndSaveImageAsync(
            int userId,
            int runId,
            int sceneNumber,
            int beatIndex,
            string prompt,
            int? connectionId,
            ImagePreset preset, // 🔥 Tüm ayarlar burada
            CancellationToken ct)
        {
            // 1. AI İstemcisi
            var aiClient = await _aiFactory.ResolveImageClientAsync(userId, connectionId, ct);

            // 2. Kayıt Klasörü
            var outputDir = await _dirService.GetRunDirectoryAsync(userId, runId, "images");

            // 3. ÜRETİM (Verileri Preset içinden söküyoruz)
            var imageBytes = await AiImageRetryPolicy.GenerateImageAsync(
                aiClient: aiClient,
                operationLabel: $"Sahne {sceneNumber} / Beat {beatIndex} manuel görsel yenileme",
                prompt: ImagePromptComposer.EnsureTextHandlingRule(prompt),
                negativePrompt: ImagePromptComposer.StrengthenNegativePrompt(preset.NegativePrompt),
                size: preset.Size,
                style: preset.ArtStyle,
                model: preset.ModelName,
                logAsync: _ => Task.CompletedTask,
                ct: ct
            );

            // 4. Kaydet
            var fileName = $"scene_{sceneNumber:00}_b{Math.Max(1, beatIndex):00}_{Guid.NewGuid().ToString()[..6]}.png";
            var fullPath = Path.Combine(outputDir, fileName);

            await File.WriteAllBytesAsync(fullPath, imageBytes, ct);

            return fullPath;
        }
    }
}
