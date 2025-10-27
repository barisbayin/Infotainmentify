using Application.Contracts.Enums;
using Core.Enums;

namespace Application.Contracts.Ai
{
    public sealed record UserAiConnectionDetailDto(
        int Id,
        string Name,
        AiProviderType Provider,
        AiAuthType AuthType,
        AiCapability Capabilities,
        bool IsDefaultForText,
        bool IsDefaultForImage,
        bool IsDefaultForEmbedding,
        // Aşağıdaki iki alanın doluluğu exposure’a bağlı
        IReadOnlyDictionary<string, string>? Credentials,        // Masked veya Plain değerler
        CredentialExposure CredentialsExposure                  // Hangisi döndü
    );
}
