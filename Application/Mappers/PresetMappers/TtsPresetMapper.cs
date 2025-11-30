using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class TtsPresetMapper
    {
        public static TtsPresetListDto ToListDto(this TtsPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            VoiceId = e.VoiceId,
            LanguageCode = e.LanguageCode,
            UpdatedAt = e.UpdatedAt
        };

        public static TtsPresetDetailDto ToDetailDto(this TtsPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            UserAiConnectionId = e.UserAiConnectionId,
            VoiceId = e.VoiceId,
            LanguageCode = e.LanguageCode,
            EngineModel = e.EngineModel,
            SpeakingRate = e.SpeakingRate,
            Pitch = e.Pitch,
            Stability = e.Stability,
            Clarity = e.Clarity,
            StyleExaggeration = e.StyleExaggeration,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
