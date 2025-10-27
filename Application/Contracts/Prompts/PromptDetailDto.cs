namespace Application.Contracts.Prompts
{
    public sealed class PromptDetailDto
    {
        public int Id { get; set; }                 // 0 => create, >0 => update
        public string Name { get; set; } = default!;
        public string? Category { get; set; }
        public string? Language { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string Body { get; set; } = default!;
    }
}
