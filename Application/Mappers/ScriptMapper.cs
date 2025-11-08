using Application.Contracts.Script;
using Application.Contracts.Story;
using Core.Entity;

namespace Application.Mappers
{
    public static class ScriptMapper
    {
        public static ScriptListDto ToListDto(this Script e)
            => new()
            {
                Id = e.Id,
                Title = e.Title,
                Summary = e.Summary,
                Language = e.Language,
                RenderStyle = e.RenderStyle,
                ProductionType = e.ProductionType,
                PromptName = e.Prompt?.Name,
                AiProvider = e.AiConnection?.Provider.ToString(),
                ModelName = e.MetaJson?.Contains("model") == true
                    ? ExtractModelFromMeta(e.MetaJson)
                    : e.AiConnection?.TextModel,
                TopicCode = e.Topic?.TopicCode,
                TopicPremise = e.Topic?.Premise,
                CreatedAt = e.CreatedAt
            };

        public static ScriptDetailDto ToDetailDto(this Script e)
            => new()
            {
                Id = e.Id,
                Title = e.Title,
                Content = e.Content,
                Summary = e.Summary,
                Language = e.Language,
                RenderStyle = e.RenderStyle,
                ProductionType = e.ProductionType,
                TopicId = e.TopicId,
                PromptId = e.PromptId,
                AiConnectionId = e.AiConnectionId,
                ScriptGenerationProfileId = e.ScriptGenerationProfileId,
                TopicCode = e.Topic?.TopicCode,
                TopicPremise = e.Topic?.Premise,
                PromptName = e.Prompt?.Name,
                AiProvider = e.AiConnection?.Provider.ToString(),
                ModelName = e.AiConnection?.TextModel,
                ProfileName = e.ScriptGenerationProfile?.ProfileName,
                MetaJson = e.MetaJson,
                ScriptJson = e.ScriptJson,
                ResponseTimeMs = e.ResponseTimeMs,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };

        private static string? ExtractModelFromMeta(string metaJson)
        {
            try
            {
                var meta = System.Text.Json.JsonDocument.Parse(metaJson);
                if (meta.RootElement.TryGetProperty("model", out var val))
                    return val.GetString();
            }
            catch { /* ignore */ }
            return null;
        }
    }
}

