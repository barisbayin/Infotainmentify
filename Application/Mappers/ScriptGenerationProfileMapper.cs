using Application.Contracts.Script;
using Core.Entity;

namespace Application.Mappers
{
    public static class ScriptGenerationProfileMapper
    {
        // ---------------- LIST ----------------
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

                // 🔗 Ana AI bağlantısı
                AiConnectionId = e.AiConnectionId,
                AiConnectionName = e.AiConnection?.Name ?? "-",
                AiProvider = e.AiConnection?.Provider.ToString() ?? "-",

                // 🎨 Görsel AI
                ImageAiConnectionId = e.ImageAiConnectionId,
                ImageAiConnectionName = e.ImageAiConnection?.Name,

                // 🗣️ TTS AI
                TtsAiConnectionId = e.TtsAiConnectionId,
                TtsAiConnectionName = e.TtsAiConnection?.Name,

                // 🎬 Video AI
                VideoAiConnectionId = e.VideoAiConnectionId,
                VideoAiConnectionName = e.VideoAiConnection?.Name,

                // 🔗 Prompt & Topic Profili
                PromptId = e.PromptId,
                PromptName = e.Prompt?.Name ?? "-",
                TopicGenerationProfileId = e.TopicGenerationProfileId,
                TopicGenerationProfileName = e.TopicGenerationProfile?.ProfileName
            };
        }

        // ---------------- DETAIL ----------------
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

                // 🎨 Görsel üretim
                ImageAiConnectionId = e.ImageAiConnectionId,
                ImageModelName = e.ImageModelName,
                ImageRenderStyle = e.ImageRenderStyle,
                ImageAspectRatio = e.ImageAspectRatio,

                // 🗣️ TTS üretim
                TtsAiConnectionId = e.TtsAiConnectionId,
                TtsModelName = e.TtsModelName,
                TtsVoice = e.TtsVoice,

                // 🎬 Video üretim
                VideoAiConnectionId = e.VideoAiConnectionId,
                VideoModelName = e.VideoModelName,
                VideoTemplate = e.VideoTemplate,

                // 🔄 Flags
                AutoGenerateAssets = e.AutoGenerateAssets,
                AutoRenderVideo = e.AutoRenderVideo,

                // 🧾 Görüntüleme amaçlı adlar
                PromptName = e.Prompt?.Name ?? "-",
                AiConnectionName = e.AiConnection?.Name ?? "-",
                TopicGenerationProfileName = e.TopicGenerationProfile?.ProfileName,
                ImageAiConnectionName = e.ImageAiConnection?.Name,
                TtsAiConnectionName = e.TtsAiConnection?.Name,
                VideoAiConnectionName = e.VideoAiConnection?.Name
            };
        }
    }
}
