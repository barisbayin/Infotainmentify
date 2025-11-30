using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Prompts
{
    public class SavePromptDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(10)]
        public string? Language { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Required] // Prompt metni zorunlu
        public string Body { get; set; } = default!;

        [MaxLength(2000)]
        public string? SystemPrompt { get; set; }
    }
}
