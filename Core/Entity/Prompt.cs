using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class Prompt : BaseEntity
    {
        // 👇 user bazlı sahiplik
        public int UserId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = default!;

        [MaxLength(64)]
        public string? Category { get; set; }

        [MaxLength(10)]
        public string? Language { get; set; }  // "tr","en"...

        // 👇 Türkçe açıklama / ne işe yarar
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]                     // Prompt şablonu (örn. Scriban/Plain)
        public string Body { get; set; } = default!;

        public string? SystemPrompt { get; set; }
    }
}
