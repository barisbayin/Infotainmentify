namespace Application.Contracts.Pipeline
{
    public class PipelineTemplateListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ConceptName { get; set; } = default!; // Hangi konsept?
        public int StageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool AutoPublish { get; set; } = false;
    }
}
