using Application.Contracts.Pipeline;
using Application.Models;

namespace Application.Services.Interfaces
{
    public interface IContentPipelineService
    {
        Task<int> CreateRunAsync(int userId, CreatePipelineRunRequest request, CancellationToken ct);
        Task StartRunAsync(int userId, int runId, CancellationToken ct);
        Task<PipelineRunDetailDto?> GetRunDetailsAsync(int userId, int runId, CancellationToken ct);
        Task<IEnumerable<PipelineRunListDto>> ListRunsAsync(int userId, int? conceptId, CancellationToken ct);
        Task RetryStageAsync(int userId, int runId, string stageType, int? newPresetId = null, CancellationToken ct = default);
        Task<List<string>> GetRunLogsAsync(int runId);
        Task ApproveRunAsync(int runId, CancellationToken ct);
        Task<string> RegenerateSceneImageAsync(int runId, int sceneIndex, CancellationToken ct);
    }
}
