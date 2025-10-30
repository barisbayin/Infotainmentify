using Application.Job;
using Application.Services;
using Core.Entity;
using Core.Enums;

namespace Infrastructure.Job.JobExecutors
{
    public class TopicGenerationJobExecutor : IJobExecutor
    {
        public JobType JobType => JobType.TopicGeneration;
        private readonly TopicGenerationService _topicService;

        public TopicGenerationJobExecutor(TopicGenerationService topicService)
        {
            _topicService = topicService;
        }

        public async Task<string> ExecuteAsync(IJobProfile profile, CancellationToken ct)
        {
            var p = (TopicGenerationProfile)profile;
            var message = await _topicService.GenerateFromProfileAsync(p.Id, ct);
            return message; // <— şimdi string döndürüyor
        }

        public Task InterruptAsync(IJobProfile profile, CancellationToken ct)
        {
            Console.WriteLine($"[TopicGeneration] Durduruldu: {profile}");
            return Task.CompletedTask;
        }
    }
}
