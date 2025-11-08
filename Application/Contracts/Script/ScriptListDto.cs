namespace Application.Contracts.Story
{
    public sealed class ScriptListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Summary { get; set; }

        public string? Language { get; set; }
        public string? RenderStyle { get; set; }
        public string? ProductionType { get; set; }

        public string? PromptName { get; set; }
        public string? AiProvider { get; set; }
        public string? ModelName { get; set; }

        public string? TopicCode { get; set; }
        public string? TopicPremise { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
