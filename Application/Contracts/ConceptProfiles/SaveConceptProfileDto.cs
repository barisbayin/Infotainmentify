using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.ConceptProfiles
{
    public class SaveConceptProfileDto
    {
        [MaxLength(40)]
        public string? ProductionProfile { get; set; }

        [MaxLength(20)]
        public string? DefaultLanguage { get; set; }

        [MaxLength(60)]
        public string? DefaultPlatform { get; set; }

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

        [Range(15, 7200)]
        public int? DefaultDurationSec { get; set; }

        public int? DefaultTemplateId { get; set; }
        public string? DefaultReviewPolicyJson { get; set; }
    }
}
