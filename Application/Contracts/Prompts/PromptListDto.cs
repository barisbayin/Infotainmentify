namespace Application.Contracts.Prompts
{
    public sealed class PromptListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Category { get; set; }
        public string? Language { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
