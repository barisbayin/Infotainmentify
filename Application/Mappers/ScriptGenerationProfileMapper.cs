using Application.Contracts.Script;
using Core.Entity;

namespace Application.Mappers
{
    public static class ScriptGenerationProfileMapper
    {
        public static ScriptGenerationProfileListDto ToListDto(this ScriptGenerationProfile e)
        {
            return new ScriptGenerationProfileListDto
            {
                Id = e.Id,
                ProfileName = e.ProfileName,
                ModelName = e.ModelName,
                Language = e.Language,
                OutputMode = e.OutputMode,
                ProductionType = e.ProductionType,
                RenderStyle = e.RenderStyle,
                Temperature = e.Temperature,
                IsPublic = e.IsPublic,
                AllowRetry = e.AllowRetry,
                Status = e.Status,
                AiConnectionId = e.AiConnectionId,
                AiConnectionName = e.AiConnection.Name?.ToString(),
                AiProvider = e.AiConnection?.Provider.ToString() ?? "-",
                PromptId = e.PromptId,
                PromptName = e.Prompt?.Name ?? "-",
                TopicGenerationProfileId = e.TopicGenerationProfileId,
                TopicGenerationProfileName = e.TopicGenerationProfile?.ProfileName
            };
        }

        public static ScriptGenerationProfileDetailDto ToDetailDto(this ScriptGenerationProfile e)
        {
            return new ScriptGenerationProfileDetailDto
            {
                Id = e.Id,
                AppUserId = e.AppUserId,
                PromptId = e.PromptId,
                AiConnectionId = e.AiConnectionId,
                TopicGenerationProfileId = e.TopicGenerationProfileId,
                ProfileName = e.ProfileName,
                ModelName = e.ModelName,
                Temperature = e.Temperature,
                Language = e.Language,
                OutputMode = e.OutputMode,
                ConfigJson = e.ConfigJson,
                Status = e.Status,
                ProductionType = e.ProductionType,
                RenderStyle = e.RenderStyle,
                IsPublic = e.IsPublic,
                AllowRetry = e.AllowRetry,
                PromptName = e.Prompt?.Name ?? "-",
                AiConnectionName = e.AiConnection?.Provider.ToString(),
                TopicGenerationProfileName = e.TopicGenerationProfile?.ProfileName
            };
        }
    }
}
