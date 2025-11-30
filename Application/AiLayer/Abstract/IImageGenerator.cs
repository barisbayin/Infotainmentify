namespace Application.AiLayer.Abstract
{
    // 3. GÖRSEL UZMANI (DALL-E, Stability, Leonardo)
    public interface IImageGenerator : IBaseAiGenerator
    {
        Task<byte[]> GenerateImageAsync(
            string prompt,
            string? negativePrompt,
            string size = "1080x1920",
            string? style = null,
            string? model = null,
            CancellationToken ct = default);
    }
}

