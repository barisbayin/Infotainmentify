using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class RenderPresetMapper
    {
        public static RenderPresetListDto ToListDto(this RenderPreset e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            OutputWidth = e.OutputWidth,
            OutputHeight = e.OutputHeight,
            Fps = e.Fps,
            UpdatedAt = e.UpdatedAt
        };

        public static RenderPresetDetailDto ToDetailDto(this RenderPreset e)
        {
            // Entity'deki helper property'lerden veriyi çekiyoruz
            var cap = e.CaptionSettings;
            var aud = e.AudioMixSettings;

            return new RenderPresetDetailDto
            {
                Id = e.Id,
                Name = e.Name,
                AppUserId = e.AppUserId,
                OutputWidth = e.OutputWidth,
                OutputHeight = e.OutputHeight,
                Fps = e.Fps,
                BitrateKbps = e.BitrateKbps,
                ContainerFormat = e.ContainerFormat,

                // Nested Mapping
                CaptionSettings = new RenderCaptionSettingsDto
                {
                    EnableCaptions = cap.EnableCaptions,
                    FontName = cap.FontName,
                    FontSize = cap.FontSize,
                    PrimaryColor = cap.PrimaryColor,
                    OutlineColor = cap.OutlineColor,
                    EnableHighlight = cap.EnableHighlight,
                    HighlightColor = cap.HighlightColor,
                    MaxWordsPerLine = cap.MaxWordsPerLine
                },
                AudioMixSettings = new RenderAudioMixSettingsDto
                {
                    VoiceVolumePercent = aud.VoiceVolumePercent,
                    MusicVolumePercent = aud.MusicVolumePercent,
                    EnableDucking = aud.EnableDucking
                },

                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };
        }
    }
}
