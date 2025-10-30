using Core.Enums;

namespace Application.Contracts.Job
{
    public sealed class JobExecutionDetailDto
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string JobName { get; set; } = null!;
        public JobStatus Status { get; set; }
        public string ResultJson { get; set; } = "{}";
        public string? ErrorMessage { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
