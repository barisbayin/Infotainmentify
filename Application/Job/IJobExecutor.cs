using Core.Enums;

namespace Application.Job
{
    public interface IJobExecutor
    {
        JobType JobType { get; }

        Task<string> ExecuteAsync(IJobProfile profile, CancellationToken ct);
        Task InterruptAsync(IJobProfile profile, CancellationToken ct);
    }
}
