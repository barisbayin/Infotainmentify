namespace Application.Contracts.Pipeline
{
    // DETAIL RESPONSE (Polling için kullanılacak)
    public class PipelineRunDetailDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public List<PipelineStageDto> Stages { get; set; } = new();
    }
}
