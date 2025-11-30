using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class VideoPresetMapper
    {
        public static VideoPresetListDto ToListDto(this VideoPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            ModelName = e.ModelName,
            GenerationMode = e.GenerationMode.ToString(),
            UpdatedAt = e.UpdatedAt
        };

        public static VideoPresetDetailDto ToDetailDto(this VideoPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            UserAiConnectionId = e.UserAiConnectionId,
            ModelName = e.ModelName,
            GenerationMode = e.GenerationMode,
            AspectRatio = e.AspectRatio,
            DurationSeconds = e.DurationSeconds,
            PromptTemplate = e.PromptTemplate,
            NegativePrompt = e.NegativePrompt,
            CameraControlSettingsJson = e.CameraControlSettingsJson,
            AdvancedSettingsJson = e.AdvancedSettingsJson,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
