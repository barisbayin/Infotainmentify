namespace Application.Contracts.Script
{
    public class ScriptGenerationProfileListDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public double Temperature { get; set; }
        public string? Language { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        // İlişkiler (flatten)
        public string? PromptName { get; set; }
        public string? AiConnectionName { get; set; }
        public string? AiProvider { get; set; }
        public string? TopicGenerationProfileName { get; set; }
    }
}
