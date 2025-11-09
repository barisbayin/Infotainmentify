using Application.Abstractions;
using Application.Services;
using Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Application.Job
{
    [DisallowConcurrentExecution]
    public class BaseQuartzJob : IJob
    {
        private readonly IServiceProvider _provider;

        public BaseQuartzJob(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.MergedJobDataMap.GetInt("JobId");

            using var scope = _provider.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<JobSettingService>();
            var execService = scope.ServiceProvider.GetRequiredService<JobExecutionService>();
            var factory = scope.ServiceProvider.GetRequiredService<JobExecutorFactory>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotifierService>();
            var ct = context.CancellationToken;

            // 1️⃣ JobExecution kaydı başlat
            var exec = await execService.StartExecutionAsync(jobId, ct);

            try
            {
                // 2️⃣ JobSetting detayını al (User bağımsız)
                var jobEntity = await jobService.GetByIdNoUserAsync(jobId, ct)
                    ?? throw new Exception($"Job bulunamadı: {jobId}");

                // 3️⃣ Executor çöz
                var executor = factory.Resolve(jobEntity.JobType);

                // 4️⃣ Profile nesnesini çöz (örnek: TopicGenerationProfile)
                IJobProfile profile = await jobService.ResolveProfileAsync(jobEntity, ct)
                    ?? throw new Exception($"Profile çözümlenemedi: {jobEntity.ProfileType}");

                // 5️⃣ Çalıştır
                var message = await executor.ExecuteAsync(profile, ct);

                // 6️⃣ Başarılı olarak kaydet
                await execService.CompleteExecutionAsync(exec.Id, message, ct);

                // 7️⃣ Bildirim gönder 🎉
                await notifier.JobCompletedAsync(jobEntity.AppUserId, jobId, success: true, message);
            }
            catch (Exception ex)
            {
                // 8️⃣ Hatalı olarak kaydet
                await execService.FailExecutionAsync(exec.Id, ex.Message, ct);

                // 9️⃣ Bildirim gönder ❌
                await scope.ServiceProvider.GetRequiredService<INotifierService>()
                    .JobCompletedAsync(exec.Job.AppUserId, jobId, success: false, ex.Message);
            }
        }
    }
}
