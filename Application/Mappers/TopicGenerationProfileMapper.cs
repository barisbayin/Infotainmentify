using Application.Contracts.Topics;
using Core.Entity;

namespace Application.Mappers
{
    public static class TopicGenerationProfileMapper
    {
        public static TopicGenerationProfileListDto ToListDto(this TopicGenerationProfile e)
            => new()
            {
                Id = e.Id,
                ProfileName = e.ProfileName,
                ModelName = e.ModelName,
                PromptName = e.Prompt?.Name,
                AiProvider = e.AiConnection?.Name,
                RequestedCount = e.RequestedCount,
                Status = e.Status,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt
            };

        public static TopicGenerationProfileDetailDto ToDetailDto(this TopicGenerationProfile e)
            => new()
            {
                Id = e.Id,
                ProfileName = e.ProfileName,
                PromptId = e.PromptId,
                AiConnectionId = e.AiConnectionId,
                ModelName = e.ModelName,
                RequestedCount = e.RequestedCount,
                RawResponseJson = e.RawResponseJson,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                Status = e.Status,
                PromptName = e.Prompt?.Name,
                AiProvider = e.AiConnection?.Name
            };
    }
}
