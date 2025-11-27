using Application.Abstractions;
using Core.Entity;

namespace Application.Executors
{
    public class ContentPlanStageExecutor : IStageExecutor
    {
        public Task ExecuteAsync(ContentPipelineRun pipeline, StageConfig stage, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
