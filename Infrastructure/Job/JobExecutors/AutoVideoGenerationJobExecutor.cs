using Application.Job;
using Application.Services;
using Core.Abstractions;
using Core.Entity;
using Core.Enums;

namespace Infrastructure.Job.JobExecutors
{
    public class AutoVideoGenerationJobExecutor : IJobExecutor
    {
        public JobType JobType => JobType.AutoVideoGeneration;

        private readonly AutoVideoGenerationService _service;
        private readonly ICurrentJobContext _jobContext;

        public AutoVideoGenerationJobExecutor(
            AutoVideoGenerationService service,
            ICurrentJobContext jobContext)
        {
            _service = service;
            _jobContext = jobContext;
        }

        public async Task<string> ExecuteAsync(IJobProfile profile, CancellationToken ct)
        {
            // ---- Profile Type Validation ----
            if (profile is not VideoGenerationProfile p)
                throw new InvalidCastException("Profile tipi VideoGenerationProfile olmalıdır.");

            // ---- UserId from JobSetting ----
            var userId = _jobContext.UserId;

            // ---- Run pipeline for this user ----
            var pipeline = await _service.RunAsync(userId, p.Id, ct);

            // ---- Output ----
            return pipeline.FinalTitle ?? "AutoVideo pipeline başarıyla tamamlandı.";
        }

        public Task InterruptAsync(IJobProfile profile, CancellationToken ct)
        {
            Console.WriteLine($"[AutoVideo] Durduruldu: {profile}");
            return Task.CompletedTask;
        }
    }
}
