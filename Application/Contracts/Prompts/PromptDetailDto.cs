namespace Application.Contracts.Prompts
{
    public class PromptDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Category { get; set; }
        public string? Language { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string Body { get; set; } = default!;       // Ağır veri
        public string? SystemPrompt { get; set; }          // Ağır veri
        public DateTime CreatedAt { get; set; }
    }
}
