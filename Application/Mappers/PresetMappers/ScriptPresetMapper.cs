using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class ScriptPresetMapper
    {
        public static ScriptPresetListDto ToListDto(this ScriptPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            ModelName = e.ModelName,
            Tone = e.Tone,
            Language = e.Language,
            UpdatedAt = e.UpdatedAt
        };

        public static ScriptPresetDetailDto ToDetailDto(this ScriptPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            UserAiConnectionId = e.UserAiConnectionId,
            ModelName = e.ModelName,
            Tone = e.Tone,
            TargetDurationSec = e.TargetDurationSec,
            Language = e.Language,
            IncludeHook = e.IncludeHook,
            IncludeCta = e.IncludeCta,
            PromptTemplate = e.PromptTemplate,
            SystemInstruction = e.SystemInstruction,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
