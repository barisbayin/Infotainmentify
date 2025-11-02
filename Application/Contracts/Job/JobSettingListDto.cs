using Core.Enums;

namespace Application.Contracts.Job
{
    public sealed class JobSettingListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string JobType { get; set; } = default!;
        public bool IsAutoRunEnabled { get; set; }
        public decimal? PeriodHours { get; set; }
        public string Status { get; set; } = default!;
        public DateTimeOffset? LastRunAt { get; set; }
        public string? LastError { get; set; }
        public DateTimeOffset? LastErrorAt { get; set; }
    }
}
