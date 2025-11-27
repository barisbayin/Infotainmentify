namespace Application.AiLayer
{
    public interface ITtsGenerator
    {
        Task<byte[]> GenerateAsync(
            string text,
            string? voiceName,
            string? languageCode,
            string? modelName,
            CancellationToken ct = default);
    }
}
