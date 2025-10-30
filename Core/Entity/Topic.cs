using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class Topic : BaseEntity
    {
        public int UserId { get; set; }

        [Required, MaxLength(64)]
        public string TopicCode { get; set; } = default!; // "topic-20251021-030"

        [MaxLength(64)]
        public string? Category { get; set; }

        public string? PremiseTr { get; set; }
        public string? Premise { get; set; }

        [MaxLength(32)]
        public string? Tone { get; set; }

        public string? PotentialVisual { get; set; }

        public bool NeedsFootage { get; set; } = false;
        public bool FactCheck { get; set; } = false;

        // Basit çözüm: JSON string (SQL Server JSON fonk. ile sorgulanabilir)
        public string? TagsJson { get; set; }  // ["infotainment","shorts","aiart","fiction"]

        public string? TopicJson { get; set; }

        [ForeignKey(nameof(Prompt))]
        public int? PromptId { get; set; }
        public Prompt? Prompt { get; set; }

    }
}