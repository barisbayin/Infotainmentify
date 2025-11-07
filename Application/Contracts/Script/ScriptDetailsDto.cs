namespace Application.Contracts.Script
{
    public class ScriptDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string? Summary { get; set; }
        public string? Language { get; set; }
        public string? MetaJson { get; set; }
        public string? ScriptJson { get; set; }
        public int TopicId { get; set; }
        public bool IsActive { get; set; }
    }
}
