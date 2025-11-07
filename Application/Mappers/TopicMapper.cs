using Application.Contracts.Topics;
using Core.Entity;

namespace Application.Contracts.Mappers
{
    public static class TopicMapper
    {
        // -------------------- LIST DTO --------------------
        public static TopicListDto ToListDto(this Topic e) => new()
        {
            Id = e.Id,
            Category = e.Category,
            SubCategory = e.SubCategory,
            Tone = e.Tone,
            Premise = e.Premise,
            PremiseTr = e.PremiseTr,
            ScriptGenerated = e.ScriptGenerated,
            IsActive = e.IsActive,
            PromptId = e.PromptId,
            PromptName = e.Prompt?.Name,
            UpdatedAt = e.UpdatedAt
        };

        // -------------------- DETAIL DTO --------------------
        public static TopicDetailDto ToDetailDto(this Topic e) => new()
        {
            Id = e.Id,
            TopicCode = e.TopicCode,
            Category = e.Category,
            SubCategory = e.SubCategory,
            Series = e.Series,
            Premise = e.Premise,
            PremiseTr = e.PremiseTr,
            Tone = e.Tone,
            PotentialVisual = e.PotentialVisual,
            RenderStyle = e.RenderStyle,
            VoiceHint = e.VoiceHint,
            ScriptHint = e.ScriptHint,
            FactCheck = e.FactCheck,
            NeedsFootage = e.NeedsFootage,
            Priority = e.Priority,
            TopicJson = e.TopicJson,
            ScriptGenerated = e.ScriptGenerated,
            ScriptGeneratedAt = e.ScriptGeneratedAt,
            PromptId = e.PromptId,
            PromptName = e.Prompt?.Name,
            ScriptId = e.ScriptId,
            ScriptTitle = e.Script?.Title,
            IsActive = e.IsActive,
            UpdatedAt = e.UpdatedAt
        };
    }
}
