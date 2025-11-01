namespace Application.Contracts.Job
{
    public sealed class JobSettingDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string JobType { get; set; } = default!;
        public int ProfileId { get; set; }
        public string ProfileType { get; set; } = default!;
        public bool IsAutoRunEnabled { get; set; }
        public decimal? PeriodHours { get; set; }
        public string? Status { get; set; } = default!;
        public string? LastError { get; set; }
        public DateTimeOffset? LastRunAt { get; set; }
    }
}
