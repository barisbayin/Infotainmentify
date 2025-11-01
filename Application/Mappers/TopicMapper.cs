using Application.Contracts.Topics;
using Core.Entity;

namespace Application.Contracts.Mappers
{
    public static class TopicMapper
    {
        public static TopicListDto ToListDto(this Topic e) => new()
        {
            Id = e.Id,
            TopicCode = e.TopicCode,
            Category = e.Category,
            Tone = e.Tone,
            NeedsFootage = e.NeedsFootage,
            FactCheck = e.FactCheck,
            IsActive = e.IsActive,
            UpdatedAt = e.UpdatedAt,
            PromptId = e.PromptId,
            PromptTitle = e.Prompt?.Name, // Prompt navigation varsa
            PremiseTr = e.PremiseTr,
            Premise = e.Premise
        };

        public static TopicDetailDto ToDetailDto(this Topic e) => new()
        {
            Id = e.Id,
            TopicCode = e.TopicCode,
            Category = e.Category,
            PremiseTr = e.PremiseTr,
            Premise = e.Premise,
            Tone = e.Tone,
            PotentialVisual = e.PotentialVisual,
            NeedsFootage = e.NeedsFootage,
            FactCheck = e.FactCheck,
            TagsJson = e.TagsJson,
            TopicJson = e.TopicJson,
            PromptId = e.PromptId,
            IsActive = e.IsActive
        };
    }
}
