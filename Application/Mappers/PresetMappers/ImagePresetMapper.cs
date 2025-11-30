using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class ImagePresetMapper
    {
        public static ImagePresetListDto ToListDto(this ImagePreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            ModelName = e.ModelName,
            Size = e.Size,
            UpdatedAt = e.UpdatedAt
        };

        public static ImagePresetDetailDto ToDetailDto(this ImagePreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            UserAiConnectionId = e.UserAiConnectionId,
            ModelName = e.ModelName,
            ArtStyle = e.ArtStyle,
            Size = e.Size,
            Quality = e.Quality,
            PromptTemplate = e.PromptTemplate,
            NegativePrompt = e.NegativePrompt,
            ImageCountPerScene = e.ImageCountPerScene,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
