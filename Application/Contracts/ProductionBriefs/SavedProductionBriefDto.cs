namespace Application.Contracts.ProductionBriefs
{
    public class SavedProductionBriefDto
    {
        public int Id { get; set; }
        public int? ConceptId { get; set; }
        public string? ConceptName { get; set; }
        public string Name { get; set; } = default!;
        public string? MainTitle { get; set; }
        public string? Angle { get; set; }
        public string? Audience { get; set; }
        public string? TargetDuration { get; set; }
        public string? MustCover { get; set; }
        public string? Avoid { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }
}
