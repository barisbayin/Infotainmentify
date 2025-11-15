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

        public AutoVideoGenerationJobExecutor(AutoVideoGenerationService service)
        {
            _service = service;
        }

        public async Task<string> ExecuteAsync(IJobProfile profile, CancellationToken ct)
        {
            // ---- Profile Type Validation ----
            if (profile is not VideoGenerationProfile p)
                throw new InvalidCastException("Profile tipi AutoVideoAssetProfile olmalıdır.");

            // ---- Pipeline ----
            var result = await _service.RunAsync(p.Id, ct);

            // ---- Output ----
            return result.FinalTitle ?? "AutoVideo pipeline başarıyla tamamlandı.";
        }

        public Task InterruptAsync(IJobProfile profile, CancellationToken ct)
        {
            Console.WriteLine($"[AutoVideo] Durduruldu: {profile}");
            return Task.CompletedTask;
        }
    }
}
