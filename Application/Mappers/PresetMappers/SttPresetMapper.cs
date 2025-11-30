using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class SttPresetMapper
    {
        public static SttPresetListDto ToListDto(this SttPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            ModelName = e.ModelName,
            LanguageCode = e.LanguageCode,
            UpdatedAt = e.UpdatedAt
        };

        public static SttPresetDetailDto ToDetailDto(this SttPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            UserAiConnectionId = e.UserAiConnectionId,
            ModelName = e.ModelName,
            LanguageCode = e.LanguageCode,
            EnableWordLevelTimestamps = e.EnableWordLevelTimestamps,
            EnableSpeakerDiarization = e.EnableSpeakerDiarization,
            OutputFormat = e.OutputFormat,
            Prompt = e.Prompt,
            Temperature = e.Temperature,
            FilterProfanity = e.FilterProfanity,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
