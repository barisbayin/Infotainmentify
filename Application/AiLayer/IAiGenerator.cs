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

        // ======================================================
        // 🧠 METİN ÜRETİMİ
        // ======================================================

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

        // ======================================================
        // 🎨 GÖRSEL ÜRETİMİ
        // ======================================================
        Task<byte[]> GenerateImageAsync(
            string prompt,
            string size = "1080x1920",
            string? style = null,
            string? model = null,
            CancellationToken ct = default);

        // ======================================================
        // 🎙️ SES (TTS) ÜRETİMİ
        // ======================================================
        /// <summary>
        /// Metinden ses üretir (TTS). Dönüş değeri MP3/WAV formatlı byte dizisidir.
        /// </summary>
        Task<byte[]> GenerateAudioAsync(
            string text,
            string? voice = null,
            string? model = null,
            string? format = "mp3",
            CancellationToken ct = default);

        // ======================================================
        // 🔤 EMBEDDING (vektör temsili)
        // ======================================================
        Task<float[]> GetEmbeddingAsync(
            string text,
            string? model = null,
            CancellationToken ct = default);

        // ======================================================
        // 💚 HEALTH CHECK
        // ======================================================
        Task<bool> TestConnectionAsync(CancellationToken ct = default);
    }
}
