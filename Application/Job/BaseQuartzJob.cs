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
            var jobContext = scope.ServiceProvider.GetRequiredService<ICurrentJobContext>(); // 🔥

            var ct = context.CancellationToken;

            // ------------------------------------------------------
            // 1) Execution kaydı başlat
            // ------------------------------------------------------
            var exec = await execService.StartExecutionAsync(jobId, ct);

            try
            {
                // ------------------------------------------------------
                // 2) JobSetting kullanıcı bağımsız çekiliyor
                // ------------------------------------------------------
                var jobEntity = await jobService.GetByIdNoUserAsync(jobId, ct)
                    ?? throw new Exception($"Job bulunamadı: {jobId}");

                // ------------------------------------------------------
                // 3) 🔥 UserId ve Setting = ICurrentJobContext içine yaz
                // ------------------------------------------------------
                jobContext.UserId = jobEntity.AppUserId;
                jobContext.Setting = jobEntity;

                // ------------------------------------------------------
                // 4) Executor çöz
                // ------------------------------------------------------
                var executor = factory.Resolve(jobEntity.JobType);

                // ------------------------------------------------------
                // 5) Profile çöz (örn: ScriptGenerationProfile)
                // ------------------------------------------------------
                IJobProfile profile = await jobService.ResolveProfileAsync(jobEntity, ct)
                    ?? throw new Exception($"Profile çözümlenemedi: {jobEntity.ProfileType}");

                // ------------------------------------------------------
                // 6) Executor.Execute çalıştır
                // ------------------------------------------------------
                var message = await executor.ExecuteAsync(profile, ct);

                // ------------------------------------------------------
                // 7) Başarılı olarak tamamla
                // ------------------------------------------------------
                await execService.CompleteExecutionAsync(exec.Id, message, ct);

                // UI’ya bildirim
                await notifier.JobCompletedAsync(
                    jobEntity.AppUserId,
                    jobEntity.Id,
                    success: true,
                    message
                );
            }
            catch (Exception ex)
            {
                // ------------------------------------------------------
                // 8) Hata olarak kaydet
                // ------------------------------------------------------
                await execService.FailExecutionAsync(exec.Id, ex.Message, ct);

                // UI’ya hata ilet
                await notifier.JobCompletedAsync(
                    exec.Job.AppUserId,
                    jobId,
                    success: false,
                    ex.Message
                );
            }
        }
    }
}
