using Core.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Application.AiLayer
{
    public class AiGeneratorFactory : IAiGeneratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AiGeneratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAiGenerator Resolve(AiProviderType provider, IReadOnlyDictionary<string, string> creds)
        {
            IAiGenerator gen = provider switch
            {
                AiProviderType.OpenAI => _serviceProvider.GetRequiredService<OpenAiClient>(),
                AiProviderType.GoogleVertex => _serviceProvider.GetRequiredService<GeminiAiClient>(),
                _ => throw new NotSupportedException($"Provider '{provider}' not supported.")
            };

            gen.Initialize(creds);
            return gen;
        }
    }
}
