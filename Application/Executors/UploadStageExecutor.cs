using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Entity;
using Core.Entity.Pipeline;

namespace Application.Executors
{
    public class UploadStageExecutor : IStageExecutor
    {
        public Task<StageResult> ExecuteAsync(ContentPipelineRun contentPipelineRun, StageConfig config, StageExecution execution, PipelineContext context, CancellationToken ct, Func<string, Task>? logCallback = null)
        {
            throw new NotImplementedException();
        }

        public Task<object?> ProcessAsync(ContentPipelineRun run, StageConfig config, StageExecution exec, PipelineContext context, object? presetObj, Func<string, Task> logAsync, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
