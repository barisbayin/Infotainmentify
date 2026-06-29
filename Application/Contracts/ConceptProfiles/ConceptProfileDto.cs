namespace Application.Contracts.ConceptProfiles
{
    public class ConceptProfileDto
    {
        public int Id { get; set; }
        public int ConceptId { get; set; }
        public string? ConceptName { get; set; }
        public string ProductionProfile { get; set; } = "LongForm";
        public string DefaultLanguage { get; set; } = "en-US";
        public string DefaultPlatform { get; set; } = "YouTube";
        public string? Audience { get; set; }
        public string? Tone { get; set; }
        public string? ChannelPromise { get; set; }
        public string? VisualStyleName { get; set; }
        public string? VisualStyleBible { get; set; }
        public string? CharacterBible { get; set; }
        public string? TextPolicy { get; set; }
        public string? ContentRules { get; set; }
        public int? DefaultDurationSec { get; set; }
        public int? DefaultTemplateId { get; set; }
        public string? DefaultTemplateName { get; set; }
        public string? DefaultReviewPolicyJson { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool Exists { get; set; }
    }
}
