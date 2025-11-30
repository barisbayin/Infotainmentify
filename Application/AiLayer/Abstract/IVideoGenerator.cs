namespace Application.AiLayer.Abstract
{
    public interface IVideoGenerator : IBaseAiGenerator
    {
        // Text-to-Video veya Image-to-Video
        Task<string> GenerateVideoAsync(
            string prompt,
            string? imageUrl = null, // Eğer null değilse Image-to-Video çalışır
            string? model = null,
            int durationSeconds = 5,
            CancellationToken ct = default);
    }
}
