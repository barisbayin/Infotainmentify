using Application.Abstractions;
using Core.Entity;

namespace Application.Executors
{
    public class ImageStageExecutor : IStageExecutor
    {
        public Task ExecuteAsync(ContentPipelineRun pipeline, StageConfig stage, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

}
