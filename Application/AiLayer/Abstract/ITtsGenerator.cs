namespace Application.AiLayer.Abstract
{
    // 4. SESLENDİRME UZMANI (ElevenLabs, Google TTS)
    public interface ITtsGenerator : IBaseAiGenerator
    {
        Task<byte[]> GenerateAudioAsync(
            string text,
            string voiceName,
            string languageCode,
            string modelName,
            string ratePercent,
            string pitchString,
            string audioEncoding = "MP3",
            CancellationToken ct = default);
    }
}
