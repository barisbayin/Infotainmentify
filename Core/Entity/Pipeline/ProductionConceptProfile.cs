using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Pipeline
{
    public class ProductionConceptProfile : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int ConceptId { get; set; }
        public Concept Concept { get; set; } = null!;

        [MaxLength(40)]
        public string ProductionProfile { get; set; } = "LongForm";

        [MaxLength(20)]
        public string DefaultLanguage { get; set; } = "en-US";

        [MaxLength(60)]
        public string DefaultPlatform { get; set; } = "YouTube";

        [MaxLength(800)]
        public string? Audience { get; set; }

        [MaxLength(800)]
        public string? Tone { get; set; }

        [MaxLength(1200)]
        public string? ChannelPromise { get; set; }

        [MaxLength(160)]
        public string? VisualStyleName { get; set; }

        public string? VisualStyleBible { get; set; }
        public string? CharacterBible { get; set; }
        public string? TextPolicy { get; set; }
        public string? ContentRules { get; set; }

        public int? DefaultDurationSec { get; set; }
        public int? DefaultTemplateId { get; set; }
        public ContentPipelineTemplate? DefaultTemplate { get; set; }

        public string? DefaultReviewPolicyJson { get; set; }
    }
}
