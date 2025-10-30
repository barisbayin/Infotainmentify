using Application.Job;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Quartz;
namespace Application.Services
{
    public class JobSchedulerService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IRepository<JobSetting> _jobRepo;
        private readonly ICurrentUserService _current;

        public JobSchedulerService(
            ISchedulerFactory schedulerFactory,
            IRepository<JobSetting> jobRepo,
            ICurrentUserService current)
        {
            _schedulerFactory = schedulerFactory;
            _jobRepo = jobRepo;
            _current = current;
        }

        /// <summary>
        /// Sistemdeki tüm otomatik jobları yükler ve schedule eder.
        /// </summary>
        public async Task ScheduleAllAutoJobsAsync(CancellationToken ct)
        {
            var jobs = await _jobRepo.FindAsync(
                x => x.AppUserId == _current.UserId && x.IsAutoRunEnabled,
                asNoTracking: true,
                ct: ct);

            foreach (var job in jobs)
                await ScheduleJobAsync(job, ct);
        }

        /// <summary>
        /// Belirli bir job'u (örneğin güncellenmiş) yeniden planlar.
        /// </summary>
        public async Task RescheduleJobAsync(JobSetting job, CancellationToken ct)
        {
            var scheduler = await _schedulerFactory.GetScheduler(ct);
            var jobKey = new JobKey($"job_{job.Id}", "default");

            if (await scheduler.CheckExists(jobKey, ct))
                await scheduler.DeleteJob(jobKey, ct);

            if (job.IsAutoRunEnabled)
                await ScheduleJobAsync(job, ct);
        }

        /// <summary>
        /// Job'u Quartz üzerinde planlar.
        /// </summary>
        public async Task ScheduleJobAsync(JobSetting job, CancellationToken ct)
        {
            var scheduler = await _schedulerFactory.GetScheduler(ct);

            var jobDetail = JobBuilder.Create<BaseQuartzJob>()
                .WithIdentity($"job_{job.Id}", "default")
                .UsingJobData("JobId", job.Id)
                .Build();

            // periodHours null veya 0 ise default 24 saat
            var hours = job.PeriodHours ?? 24m;
            var intervalMs = TimeSpan.FromHours((double)hours);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger_{job.Id}", "default")
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(intervalMs).RepeatForever())
                .Build();

            await scheduler.ScheduleJob(jobDetail, trigger, ct);

            Console.WriteLine($"[Quartz] Auto job planlandı → {job.Name} ({hours} saatte bir)");
        }

        /// <summary>
        /// Job'un planını Quartz'tan kaldırır.
        /// </summary>
        public async Task UnscheduleJobAsync(int jobId, CancellationToken ct)
        {
            var scheduler = await _schedulerFactory.GetScheduler(ct);
            var jobKey = new JobKey($"job_{jobId}", "default");

            if (await scheduler.CheckExists(jobKey, ct))
            {
                await scheduler.DeleteJob(jobKey, ct);
                Console.WriteLine($"[Quartz] Job silindi → {jobId}");
            }
        }
    }
}
