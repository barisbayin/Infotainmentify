using Application.Models;

namespace Application.AiLayer.Abstract
{
    // 5. DUYMA/ÇEVİRİ UZMANI (Whisper)
    public interface ISttGenerator : IBaseAiGenerator
    {
        Task<SpeechToTextResult> SpeechToTextAsync(
            byte[] audioData,
            string languageCode = "en-US",
            string? model = null,
            CancellationToken ct = default);
    }
}
