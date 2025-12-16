namespace Application.Abstractions
{
    public interface IContentPipelineRunner
    {
        Task RunAsync(int pipelineRunId, CancellationToken ct);
        Task RetryStageAsync(int runId, string stageTypeStr, int? newPresetId = null, CancellationToken ct = default);
    }
}
