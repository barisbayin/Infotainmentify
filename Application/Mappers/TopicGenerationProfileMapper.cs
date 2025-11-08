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
                ProductionType = e.ProductionType,
                RenderStyle = e.RenderStyle,
                Language = e.Language,
                RequestedCount = e.RequestedCount,
                AutoGenerateScript = e.AutoGenerateScript,
                IsPublic = e.IsPublic,
                AllowRetry = e.AllowRetry
            };

        public static TopicGenerationProfileDetailDto ToDetailDto(this TopicGenerationProfile e)
            => new()
            {
                Id = e.Id,
                ProfileName = e.ProfileName,
                PromptId = e.PromptId,
                AiConnectionId = e.AiConnectionId,
                ModelName = e.ModelName,
                ProductionType = e.ProductionType,
                RenderStyle = e.RenderStyle,
                Language = e.Language,
                Temperature = e.Temperature,
                RequestedCount = e.RequestedCount,
                MaxTokens = e.MaxTokens,
                TagsJson = e.TagsJson,
                OutputMode = e.OutputMode,
                AutoGenerateScript = e.AutoGenerateScript,
                IsPublic = e.IsPublic,
                AllowRetry = e.AllowRetry,
                PromptName = e.Prompt?.Name,
                AiProvider = e.AiConnection?.Name
            };
    }
}
