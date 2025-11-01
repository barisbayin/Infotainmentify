using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Job
{
    public static class JobBootstrapper
    {
        public static async Task InitializeAsync(IServiceProvider sp, CancellationToken ct = default)
        {
            using var scope = sp.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<JobSettingService>();

            var autoJobs = await jobService.GetAutoRunJobsAsync(ct);

            foreach (var job in autoJobs)
            {
                await jobService.RescheduleJobAsync(job, ct);
            }

            Console.WriteLine($"[Quartz] {autoJobs.Count} job yeniden planlandı (startup).");
        }
    }
}
