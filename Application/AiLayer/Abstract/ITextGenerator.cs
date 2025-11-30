using Application.Models;

namespace Application.AiLayer.Abstract
{
    // 2. METİN UZMANI (GPT, Claude, Gemini)
    public interface ITextGenerator : IBaseAiGenerator
    {
        Task<string> GenerateTextAsync(string prompt, double temperature = 0.7, string? model = null, CancellationToken ct = default);

        Task<IReadOnlyList<TopicResult>> GenerateTopicsAsync(TopicGenerationRequest request, CancellationToken ct = default);

        Task<string> GenerateScriptsAsync(ScriptGenerationRequest request, CancellationToken ct = default);

        // Embedding genelde text modelleriyle gelir
        Task<float[]> GetEmbeddingAsync(string text, string? model = null, CancellationToken ct = default);
    }
}
