namespace Application.Contracts.Topics
{
    public sealed class TopicGenerationProfileDetailDto
    {
        public int Id { get; set; }

        public string ProfileName { get; set; } = default!;

        public int PromptId { get; set; }
        public int AiConnectionId { get; set; }

        public string ModelName { get; set; } = default!;
        public string? ProductionType { get; set; }
        public string? RenderStyle { get; set; }

        public string Language { get; set; } = "en";
        public float Temperature { get; set; } = 0.7f;
        public int RequestedCount { get; set; } = 30;
        public int? MaxTokens { get; set; }

        public string? TagsJson { get; set; }
        public string OutputMode { get; set; } = "Topic";
        public bool AutoGenerateScript { get; set; } = false;

        public bool IsPublic { get; set; } = false;
        public bool AllowRetry { get; set; } = true;

        // --- Display alanları (okuma amaçlı)
        public string? PromptName { get; set; }
        public string? AiProvider { get; set; }
    }
}
