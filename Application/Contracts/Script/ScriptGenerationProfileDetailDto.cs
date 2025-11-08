namespace Application.Contracts.Script
{
    public class ScriptGenerationProfileDetailDto
    {
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public int PromptId { get; set; }
        public int AiConnectionId { get; set; }
        public int? TopicGenerationProfileId { get; set; }

        public string ProfileName { get; set; } = default!;
        public string ModelName { get; set; } = default!;

        public float Temperature { get; set; }
        public string Language { get; set; } = default!;
        public string OutputMode { get; set; } = default!;
        public string? ConfigJson { get; set; }
        public string Status { get; set; } = default!;
        public string? ProductionType { get; set; }
        public string? RenderStyle { get; set; }

        public bool IsPublic { get; set; }
        public bool AllowRetry { get; set; }

        // Readonly related display names
        public string? PromptName { get; set; }
        public string? AiConnectionName { get; set; }
        public string? TopicGenerationProfileName { get; set; }
    }
}
