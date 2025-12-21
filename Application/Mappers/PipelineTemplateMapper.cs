using Application.Contracts.Pipeline;
using Core.Entity.Pipeline;

namespace Application.Mappers
{
    public static class PipelineTemplateMapper
    {
        public static PipelineTemplateListDto ToListDto(this ContentPipelineTemplate e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            ConceptName = e.Concept?.Name ?? "-", // Concept Include edilmeli
            StageCount = e.StageConfigs?.Count ?? 0,
            CreatedAt = e.CreatedAt,
            AutoPublish = e.AutoPublish
        };

        public static PipelineTemplateDetailDto ToDetailDto(this ContentPipelineTemplate e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            ConceptId = e.ConceptId,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            AutoPublish = e.AutoPublish,
            Stages = e.StageConfigs?
                .OrderBy(s => s.Order)
                .Select(s => new StageConfigDto
                {
                    Id = s.Id,
                    StageType = s.StageType.ToString(),
                    Order = s.Order,
                    PresetId = s.PresetId,
                    OptionsJson = s.OptionsJson
                }).ToList() ?? new List<StageConfigDto>()
        };
    }
}
