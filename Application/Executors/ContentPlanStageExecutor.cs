using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Entity.Pipeline;

namespace Application.Executors
{
    public class ContentPlanStageExecutor : IStageExecutor
    {
        public Task<StageResult> ExecuteAsync(ContentPipelineRun contentPipelineRun, StageConfig config, StageExecution execution, PipelineContext context, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
