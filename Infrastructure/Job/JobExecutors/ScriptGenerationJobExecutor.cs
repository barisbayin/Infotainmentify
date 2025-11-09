using Application.Job;
using Application.Services;
using Core.Abstractions;
using Core.Entity;
using Core.Enums;

namespace Infrastructure.Job.JobExecutors
{
    public class ScriptGenerationJobExecutor : IJobExecutor
    {
        public JobType JobType => JobType.ScriptGeneration;

        private readonly ScriptGenerationService _scriptService;

        public ScriptGenerationJobExecutor(ScriptGenerationService scriptService)
        {
            _scriptService = scriptService;
        }

        public async Task<string> ExecuteAsync(IJobProfile profile, CancellationToken ct)
        {
            if (profile is not ScriptGenerationProfile p)
                throw new InvalidCastException("Profile tipi ScriptGenerationProfile olmalıdır.");

            // Scriptleri üret
            var message = await _scriptService.GenerateFromProfileAsync(p.Id, ct);
            return message.SuccessCount.ToString() ?? "Script üretimi tamamlandı.";
        }

        public Task InterruptAsync(IJobProfile profile, CancellationToken ct)
        {
            Console.WriteLine($"[ScriptGeneration] Durduruldu: {profile}");
            return Task.CompletedTask;
        }
    }
}
