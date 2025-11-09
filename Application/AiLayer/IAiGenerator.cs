using Application.Models;
using Core.Enums;

namespace Application.AiLayer
{
    /// <summary>
    /// Ortak AI sağlayıcı arabirimi (OpenAI, Gemini, Anthropic vb.)
    /// </summary>
    public interface IAiGenerator
    {
        // 🔹 Sağlayıcı tipi (örnek: GoogleVertex, OpenAi)
        AiProviderType ProviderType { get; }

        // 🔹 Credential bilgilerini runtime’da initialize eder
        void Initialize(IReadOnlyDictionary<string, string> credentials);

        // -------------------------------
        // 🧠 Metin Üretimi
        // -------------------------------

        /// <summary>
        /// Basit text üretimi (tek prompt).
        /// </summary>
        Task<string> GenerateTextAsync(
            string prompt,
            double temperature = 0.7,
            string? model = null,
            CancellationToken ct = default);

        /// <summary>
        /// Yapılandırılmış topic üretimi (structured JSON output).
        /// </summary>
        Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(
            TopicGenerationRequest request,
            CancellationToken ct = default);


        /// <summary>
        /// Yapılandırılmış script üretimi (structured JSON output).
        /// </summary>
        Task<string> GenerateScriptsAsync(
            ScriptGenerationRequest request,
            CancellationToken ct = default);

        // -------------------------------
        // 🎨 Görsel Üretimi
        // -------------------------------
        Task<byte[]> GenerateImageAsync(
            string prompt,
            string size = "1024x1024",
            string? style = null,
            CancellationToken ct = default);

        // -------------------------------
        // 🔤 Embedding (vektör temsili)
        // -------------------------------
        Task<float[]> GetEmbeddingAsync(
            string text,
            string? model = null,
            CancellationToken ct = default);

        // -------------------------------
        // 💚 Health Check
        // -------------------------------
        Task<bool> TestConnectionAsync(CancellationToken ct = default);
    }
}
