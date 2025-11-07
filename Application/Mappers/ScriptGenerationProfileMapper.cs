using Application.Contracts.Script;
using Core.Entity;

namespace Application.Mappers
{
    public static class ScriptGenerationProfileMapper
    {
        public static ScriptGenerationProfileListDto ToListDto(this ScriptGenerationProfile x)
        {
            return new ScriptGenerationProfileListDto
            {
                Id = x.Id,
                ProfileName = x.ProfileName,
                ModelName = x.ModelName,
                Temperature = x.Temperature,
                Language = x.Language,
                Status = x.Status,
                IsActive = x.IsActive,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                PromptName = x.Prompt?.Name,
                AiConnectionName = x.AiConnection?.Name,
                AiProvider = x.AiConnection?.Provider.ToString(),
                TopicGenerationProfileName = x.TopicGenerationProfile?.ProfileName
            };
        }

        public static ScriptGenerationProfileDetailDto ToDetailsDto(this ScriptGenerationProfile x)
        {
            return new ScriptGenerationProfileDetailDto
            {
                Id = x.Id,
                ProfileName = x.ProfileName,
                PromptId = x.PromptId,
                AiConnectionId = x.AiConnectionId,
                TopicGenerationProfileId = x.TopicGenerationProfileId,
                ModelName = x.ModelName,
                Temperature = x.Temperature,
                Language = x.Language,
                TopicIdsJson = x.TopicIdsJson,
                ConfigJson = x.ConfigJson,
                RawResponseJson = x.RawResponseJson,
                Status = x.Status,
                IsActive = x.IsActive,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                PromptName = x.Prompt?.Name,
                AiConnectionName = x.AiConnection?.Name,
                AiProvider = x.AiConnection?.Provider.ToString(),
                TopicGenerationProfileName = x.TopicGenerationProfile?.ProfileName
            };
        }
    }
}
