using Application.Contracts.Script;
using Application.Contracts.Story;
using Core.Entity;

namespace Application.Mappers
{
    public static class ScriptMapper
    {
        public static ScriptDetailsDto ToDetailDto(this Script e) => new()
        {
            Id = e.Id,
            TopicId = e.TopicId,
            Title = e.Title,
            Content = e.Content,
            Summary = e.Summary,
            Language = e.Language,
            MetaJson = e.MetaJson,
            ScriptJson = e.ScriptJson,
            IsActive = e.IsActive,
        };

        public static ScriptListDto ToListDto(this Script e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            TopicId = e.TopicId,
            Language = e.Language,
            IsActive = e.IsActive,
            UpdatedAt = e.UpdatedAt
        };
    }
}

