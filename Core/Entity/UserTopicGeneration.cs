using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class UserTopicGeneration : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        [Required]
        public int PromptId { get; set; }

        [ForeignKey(nameof(PromptId))]
        public Prompt Prompt { get; set; } = null!;

        [Required]
        public int AiConnectionId { get; set; }

        [ForeignKey(nameof(AiConnectionId))]
        public UserAiConnection AiConnection { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ModelName { get; set; } = null!; // örn: gpt-4-turbo, gemini-1.5-pro

        public int RequestedCount { get; set; } // kaç topic istenmişti (örn: 30)

        [Column(TypeName = "nvarchar(max)")]
        public string RawResponseJson { get; set; } = null!; // AI'dan gelen orijinal JSON

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending | Success | Failed

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }
    }

}
