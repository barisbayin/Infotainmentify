namespace Application.Contracts.Topics
{
    public sealed class TopicGenerationProfileListDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string? PromptName { get; set; }
        public string? AiProvider { get; set; }
        public int RequestedCount { get; set; }
        public string Status { get; set; } = default!;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
