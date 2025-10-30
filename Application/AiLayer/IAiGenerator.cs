using Application.Models;
using Core.Enums;

namespace Application.AiLayer
{
    public interface IAiGenerator
    {
        AiProviderType ProviderType { get; }

        // 🔹 Credential initialization (runtime)
        void Initialize(IReadOnlyDictionary<string, string> credentials);

        // --- Text / Topic Üretimi ---
        Task<string> GenerateTextAsync(
            string prompt,
            double temperature = 0.7,
            string? model = null,
            CancellationToken ct = default);

        // Topic-specific structured JSON üretimi
        Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(
            string systemPrompt,
            string userPrompt,
            int count,
            string? model = null,
            double temperature = 0.7,
            CancellationToken ct = default);

        // --- Görsel Üretimi ---
        Task<byte[]> GenerateImageAsync(
            string prompt,
            string size = "1024x1024",
            string? style = null,
            CancellationToken ct = default);

        // --- Embedding ---
        Task<float[]> GetEmbeddingAsync(
            string text,
            string? model = null,
            CancellationToken ct = default);

        // --- Health check / credit info ---
        Task<bool> TestConnectionAsync(CancellationToken ct = default);
    }
}
