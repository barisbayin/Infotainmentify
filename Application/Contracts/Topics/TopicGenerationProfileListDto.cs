namespace Application.Contracts.Topics
{
    public sealed class TopicGenerationProfileListDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string? PromptName { get; set; }
        public string? AiProvider { get; set; }
        public string? ProductionType { get; set; }
        public string? RenderStyle { get; set; }
        public string Language { get; set; } = default!;
        public int RequestedCount { get; set; }
        public bool AutoGenerateScript { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowRetry { get; set; }
    }
}
