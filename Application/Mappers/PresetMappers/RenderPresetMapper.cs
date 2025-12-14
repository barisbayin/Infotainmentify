using Application.Contracts.Render;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class RenderPresetMapper
    {
        // Entity -> ListDto
        public static RenderPresetListDto ToListDto(this RenderPreset entity)
        {
            return new RenderPresetListDto
            {
                Id = entity.Id,
                Name = entity.Name,
                OutputWidth = entity.OutputWidth,
                OutputHeight = entity.OutputHeight,
                Fps = entity.Fps,
                EncoderPreset = entity.EncoderPreset,
                UpdatedAt = entity.UpdatedAt
            };
        }

        // Entity -> DetailDto
        public static RenderPresetDetailDto ToDetailDto(this RenderPreset entity)
        {
            return new RenderPresetDetailDto
            {
                Id = entity.Id,
                Name = entity.Name,
                OutputWidth = entity.OutputWidth,
                OutputHeight = entity.OutputHeight,
                Fps = entity.Fps,
                BitrateKbps = entity.BitrateKbps,
                ContainerFormat = entity.ContainerFormat,
                EncoderPreset = entity.EncoderPreset,

                // Helper Property'lerden (JSON) okur
                CaptionSettings = entity.CaptionSettings,
                AudioMixSettings = entity.AudioMixSettings,
                VisualEffectsSettings = entity.VisualEffectsSettings, // Yeni
                BrandingSettings = entity.BrandingSettings,           // Yeni

                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        // DTO -> Entity (Create)
        public static RenderPreset ToEntity(this SaveRenderPresetDto dto)
        {
            var entity = new RenderPreset();
            // Ortak atama metodunu çağır
            entity.UpdateEntity(dto);
            return entity;
        }

        // DTO -> Entity (Update)
        public static void UpdateEntity(this RenderPreset entity, SaveRenderPresetDto dto)
        {
            entity.Name = dto.Name;
            entity.OutputWidth = dto.OutputWidth;
            entity.OutputHeight = dto.OutputHeight;
            entity.Fps = dto.Fps;
            entity.BitrateKbps = dto.BitrateKbps;
            entity.ContainerFormat = dto.ContainerFormat;
            entity.EncoderPreset = dto.EncoderPreset;

            // Helper Property set edince JSON string güncellenir
            entity.CaptionSettings = dto.CaptionSettings;
            entity.AudioMixSettings = dto.AudioMixSettings;
            entity.VisualEffectsSettings = dto.VisualEffectsSettings;
            entity.BrandingSettings = dto.BrandingSettings;
        }
    }
}
