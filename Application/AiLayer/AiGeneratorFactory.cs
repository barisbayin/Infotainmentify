using Core.Enums;

namespace Application.AiLayer
{
    public class AiGeneratorFactory : IAiGeneratorFactory
    {
        private readonly IHttpClientFactory _httpFactory;

        public AiGeneratorFactory(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public IAiGenerator Resolve(AiProviderType provider, IReadOnlyDictionary<string, string> creds)
        {
            var http = _httpFactory.CreateClient();

            return provider switch
            {
                AiProviderType.OpenAI => new OpenAiClient(http, creds),
                AiProviderType.GoogleVertex => new GeminiAiClient(http, creds),
                _ => throw new NotSupportedException($"Provider '{provider}' not supported.")
            };
        }
    }

}
