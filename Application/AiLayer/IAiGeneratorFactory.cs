using Core.Enums;

namespace Application.AiLayer
{
    public interface IAiGeneratorFactory
    {
        IAiGenerator Resolve(AiProviderType provider, IReadOnlyDictionary<string, string> creds);
    }
}
