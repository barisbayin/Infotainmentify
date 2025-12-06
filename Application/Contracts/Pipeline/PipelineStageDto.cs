namespace Application.Contracts.Pipeline
{
    public class PipelineStageDto
    {
        public string StageType { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? Error { get; set; }
        public int DurationMs { get; set; }

        public string? OutputJson { get; set; }
    }
}
