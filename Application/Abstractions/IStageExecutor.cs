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
            CancellationToken ct, 
            Func<string, Task>? logCallback = null);
        Task<object?> ProcessAsync(ContentPipelineRun run, StageConfig config, StageExecution exec, PipelineContext context, object? presetObj, Func<string, Task> logAsync, CancellationToken ct);
    }
}
