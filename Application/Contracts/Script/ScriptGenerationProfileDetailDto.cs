namespace Application.Contracts.Script
{
    public class ScriptGenerationProfileDetailDto
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }

        // 🔹 Ana Bağlantılar
        public int PromptId { get; set; }
        public int AiConnectionId { get; set; }
        public int? TopicGenerationProfileId { get; set; }

        // 🔹 Temel Parametreler
        public string ProfileName { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public float Temperature { get; set; }
        public string Language { get; set; } = "en";
        public string OutputMode { get; set; } = "Script";
        public string? ConfigJson { get; set; }
        public string Status { get; set; } = "Pending";
        public string? ProductionType { get; set; }
        public string? RenderStyle { get; set; }

        public bool IsPublic { get; set; }
        public bool AllowRetry { get; set; }

        // --------------------------------------------------------------------
        // 🧩 Yeni Alanlar — Asset & Video Generation Settings
        // --------------------------------------------------------------------

        // 🎨 Image Generation
        public int? ImageAiConnectionId { get; set; }
        public string? ImageModelName { get; set; }
        public string? ImageRenderStyle { get; set; }
        public string? ImageAspectRatio { get; set; }

        // 🗣️ TTS Generation
        public int? TtsAiConnectionId { get; set; }
        public string? TtsModelName { get; set; }
        public string? TtsVoice { get; set; }

        // 🎬 Video Generation
        public int? VideoAiConnectionId { get; set; }
        public string? VideoModelName { get; set; }
        public string? VideoTemplate { get; set; }

        // 🔄 Otomasyon bayrakları
        public bool AutoGenerateAssets { get; set; }
        public bool AutoRenderVideo { get; set; }

        // --------------------------------------------------------------------
        // 🔖 Görüntüleme amaçlı readonly alanlar
        // --------------------------------------------------------------------
        public string? PromptName { get; set; }
        public string? AiConnectionName { get; set; }
        public string? TopicGenerationProfileName { get; set; }

        public string? ImageAiConnectionName { get; set; }
        public string? TtsAiConnectionName { get; set; }
        public string? VideoAiConnectionName { get; set; }
    }
}
