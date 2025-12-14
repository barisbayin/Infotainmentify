using Application.Models;

namespace Application.Abstractions
{
    public interface IVideoRendererService
    {
        // JSON Planını al, MP4'e çevir
        Task<string> RenderVideoAsync(SceneLayoutStagePayload layout, string outputPath, string cultureCode = "en-US", CancellationToken ct = default);
    }
}
