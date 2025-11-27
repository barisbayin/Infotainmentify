using Application.Abstractions;
using Core.Entity;

namespace Application.Executors
{
    public class TopicStageExecutor : IStageExecutor
    {
        public Task ExecuteAsync(ContentPipelineRun pipeline, StageConfig stage, CancellationToken ct)
        {
            // Topic seçme işlemi burada olacak
            return Task.CompletedTask;
        }
    }
}
