namespace Application.Contracts.Pipeline
{
    // DETAIL
    public class PipelineTemplateDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int ConceptId { get; set; }

        // Şablonun Adımları
        public List<StageConfigDto> Stages { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
