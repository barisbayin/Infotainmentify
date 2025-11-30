using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Script
{
    public class SaveScriptDto
    {
        public int? TopicId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Content { get; set; } = default!;

        // JSON string olarak gelir
        public string? ScenesJson { get; set; }

        [MaxLength(10)]
        public string LanguageCode { get; set; } = "tr-TR";

        public int EstimatedDurationSec { get; set; }
    }
}
