using Core.Entity;

namespace Application.Abstractions
{
    public interface IStageExecutor
    {
        Task ExecuteAsync(ContentPipelineRun contentPipeline, StageConfig stage, CancellationToken ct);
    }

}
