using Application.Contracts.Presets;
using Core.Entity.Presets;

namespace Application.Mappers.PresetMappers
{
    public static class TopicPresetMapper
    {
        // Entity -> List DTO (Sadece özet veriler)
        public static TopicPresetListDto ToListDto(this TopicPreset e)
        {
            return new TopicPresetListDto
            {
                Id = e.Id,
                Name = e.Name,
                ModelName = e.ModelName,
                Language = e.Language,
                UpdatedAt = e.UpdatedAt
            };
        }

        // Entity -> Detail DTO (Her şey dahil)
        public static TopicPresetDetailDto ToDetailDto(this TopicPreset e)
        {
            return new TopicPresetDetailDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                UserAiConnectionId = e.UserAiConnectionId,
                ModelName = e.ModelName,
                Temperature = e.Temperature,
                Language = e.Language,
                PromptTemplate = e.PromptTemplate,
                ContextKeywordsJson = e.ContextKeywordsJson,
                SystemInstruction = e.SystemInstruction,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };
        }
    }
}
