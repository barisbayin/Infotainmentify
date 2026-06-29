using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.ProductionBriefs
{
    public class SaveProductionBriefDto
    {
        public int? ConceptId { get; set; }

        [Required, MaxLength(160)]
        public string Name { get; set; } = default!;

        [MaxLength(300)]
        public string? MainTitle { get; set; }

        [MaxLength(1000)]
        public string? Angle { get; set; }

        [MaxLength(500)]
        public string? Audience { get; set; }

        [MaxLength(100)]
        public string? TargetDuration { get; set; }

        [MaxLength(2500)]
        public string? MustCover { get; set; }

        [MaxLength(1500)]
        public string? Avoid { get; set; }

        [MaxLength(2500)]
        public string? Notes { get; set; }
    }
}
