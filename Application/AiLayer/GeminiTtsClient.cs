using Google.Cloud.TextToSpeech.V1;

namespace Application.AiLayer
{
    public sealed class GeminiTtsClient
    {
        private readonly TextToSpeechClient _client;

        public GeminiTtsClient(string? credentialFilePath = null)
        {
            if (!string.IsNullOrWhiteSpace(credentialFilePath))
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialFilePath);

            _client = TextToSpeechClient.Create();
        }

        /// <summary>
        /// Gemini 2.5 tabanlı TTS üretimi (örnek ses: Charon, Algenib)
        /// </summary>
        public async Task<string> SynthesizeAsync(
            string prompt,
            string text,
            string outputPath,
            string model = "gemini-2.5-pro-tts",
            string voiceName = "Charon",
            string languageCode = "en-US",
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var request = new SynthesizeSpeechRequest
            {
                Input = new SynthesisInput
                {
                    Text = text,
                    Prompt = prompt // 💡 yeni parametre
                },
                Voice = new VoiceSelectionParams
                {
                    LanguageCode = languageCode,
                    Name = voiceName,
                    ModelName = model
                },
                AudioConfig = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3
                }
            };

            var response = await _client.SynthesizeSpeechAsync(request, cancellationToken: ct);
            await File.WriteAllBytesAsync(outputPath, response.AudioContent.ToByteArray(), ct);

            return outputPath;
        }
    }
}
