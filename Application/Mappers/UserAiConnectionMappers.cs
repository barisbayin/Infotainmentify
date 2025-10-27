using Application.Contracts.Ai;
using Core.Entity;

namespace Application.Mappers
{
    public static class UserAiConnectionMappers
    {
        public static UserAiConnectionListDto ToListDto(this UserAiConnection x) =>
            new(
                x.Id,
                x.Name,
                x.Provider,
                x.AuthType,
                x.Capabilities,
                x.IsDefaultForText,
                x.IsDefaultForImage,
                x.IsDefaultForEmbedding
            );
    }
}
