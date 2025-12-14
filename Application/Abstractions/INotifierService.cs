namespace Application.Abstractions
{
    public interface INotifierService
    {
        Task NotifyUserAsync(int userId, string eventName, object payload);
        Task JobProgressAsync(int userId, int jobId, string status, int progress);
        Task JobCompletedAsync(int userId, int jobId, bool success, string? message = null);

        Task SendLogAsync(int runId, string message);
    }
}
