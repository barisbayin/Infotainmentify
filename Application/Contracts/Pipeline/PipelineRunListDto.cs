namespace Application.Contracts.Pipeline
{
    // Application/Contracts/Pipeline/PipelineDtos.cs

    public class PipelineRunListDto
    {
        public int Id { get; set; }
        public string TemplateName { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
