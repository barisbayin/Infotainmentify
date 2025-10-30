using Application.Services;
using Core.Enums;
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
            var ct = context.CancellationToken;

            // 1️⃣ JobExecution kaydı başlat
            var exec = await execService.StartExecutionAsync(jobId, ct);

            try
            {
                // 2️⃣ JobSetting detayını al
                var jobDetail = await jobService.GetAsync(jobId, ct);
                if (jobDetail == null)
                    throw new Exception($"Job bulunamadı: {jobId}");

                // 3️⃣ Executor çöz
                var executor = factory.Resolve(Enum.Parse<JobType>(jobDetail.JobType, true));

                // 4️⃣ Profile nesnesini çöz (örneğin TopicGenerationProfile)
                var jobEntity = await jobService.GetEntityAsync(jobId, ct)
                    ?? throw new Exception($"Job entity çözümlenemedi: {jobId}");

                var profileObj = await jobService.ResolveProfileAsync(jobEntity, ct)
                    ?? throw new Exception($"Profile çözümlenemedi: {jobDetail.ProfileType}");

                if (profileObj is not IJobProfile profile)
                    throw new Exception($"Profile '{jobDetail.ProfileType}' IJobProfile arayüzünü implemente etmiyor.");

                // 5️⃣ Job çalıştır
                var message = await executor.ExecuteAsync(profile, ct);

                // 6️⃣ Başarılı olarak kaydet
                await execService.CompleteExecutionAsync(exec.Id, message, ct);
            }
            catch (Exception ex)
            {
                // 7️⃣ Hatalı olarak kaydet
                await execService.FailExecutionAsync(exec.Id, ex.Message, ct);
            }
        }
    }
}
