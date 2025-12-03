using Application.Contracts.Topics;
using Core.Entity;

namespace Application.Contracts.Mappers
{
    public static class TopicMapper
    {
        public static TopicListDto ToListDto(this Topic e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            // Premise çok uzunsa gridde kesmek iyi olabilir
            Premise = e.Premise.Length > 100 ? e.Premise[..97] + "..." : e.Premise,
            Category = e.Category,
            Series = e.Series,
            CreatedAt = e.CreatedAt
        };

        public static TopicDetailDto ToDetailDto(this Topic e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            Premise = e.Premise,
            LanguageCode = e.LanguageCode,
            ConceptId = e.ConceptId,
            Category = e.Category,
            SubCategory = e.SubCategory,
            Series = e.Series,
            TagsJson = e.TagsJson,
            Tone = e.Tone,
            RenderStyle = e.RenderStyle,
            VisualPromptHint = e.VisualPromptHint,
            CreatedByRunId = e.CreatedByRunId,
            SourcePresetId = e.SourcePresetId,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
