using Application.Models;
using Application.Pipeline;
using Core.Entity.Pipeline;

namespace Application.Abstractions
{
    public interface IStageExecutor
    {
        // PipelineContext eklendi 👇
        Task<StageResult> ExecuteAsync(
            ContentPipelineRun contentPipelineRun,
            StageConfig config,
            StageExecution execution,
            PipelineContext context,
            CancellationToken ct);
    }
}
