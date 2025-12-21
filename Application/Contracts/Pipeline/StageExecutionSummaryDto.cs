namespace Application.Contracts.Pipeline
{
    public class StageExecutionSummaryDto
    {
        public int Id { get; set; }
        public string StageType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? OutputJson { get; set; }
    }
}
