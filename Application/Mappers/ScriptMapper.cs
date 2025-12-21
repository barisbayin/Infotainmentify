using Application.Contracts.Script;
using Application.Contracts.Story;
using Core.Entity;

namespace Application.Mappers
{
    public static class ScriptMapper
    {
        public static ScriptListDto ToListDto(this Script e) => new()
        {
            Id = e.Id,
            Title = e.Title,
            TopicTitle = e.Topic?.Title ?? "-", // Topic include edilmezse null gelebilir
            EstimatedDurationSec = e.EstimatedDurationSec,
            CreatedAt = e.CreatedAt
        };

        public static ScriptDetailDto ToDetailDto(this Script e) => new()
        {
            Id = e.Id,
            TopicId = e.TopicId,
            Title = e.Title,
            Content = e.Content,
            ScenesJson = e.ScenesJson,
            LanguageCode = e.LanguageCode,
            EstimatedDurationSec = e.EstimatedDurationSec,
            CreatedAt = e.CreatedAt,
            Description = e.Description,
            Tags = e.Tags
        };
    }
}

