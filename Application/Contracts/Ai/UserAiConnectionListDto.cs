using Core.Enums;

namespace Application.Contracts.Ai
{
    public sealed record UserAiConnectionListDto(
       int Id,
       string Name,
       AiProviderType Provider,
       AiAuthType AuthType,
       AiCapability Capabilities,
       bool IsDefaultForText,
       bool IsDefaultForImage,
       bool IsDefaultForEmbedding
   );
}
