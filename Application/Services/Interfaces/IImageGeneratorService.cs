using Core.Entity.Presets;

namespace Application.Services.Interfaces
{
    public interface IImageGeneratorService
    {
        Task<string> GenerateAndSaveImageAsync(
                int userId,
                int runId,
                int sceneNumber,
                int beatIndex,
                string prompt,
                int? connectionId,
                ImagePreset preset, // 🔥 İçinde NegativePrompt, Size, Style her şey var
                CancellationToken ct);
    }
}
