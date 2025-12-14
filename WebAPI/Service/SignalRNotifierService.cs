using Application.Abstractions;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Service
{
    public class SignalRNotifierService : INotifierService
    {
        private readonly IHubContext<NotifyHub> _hub;

        public SignalRNotifierService(IHubContext<NotifyHub> hub)
        {
            _hub = hub;
        }

        public async Task NotifyUserAsync(int userId, string eventName, object payload)
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync(eventName, payload);
        }

        public async Task JobProgressAsync(int userId, int jobId, string status, int progress)
        {
            await NotifyUserAsync(userId, "JobProgress", new
            {
                JobId = jobId,
                Status = status,
                Progress = progress
            });
        }

        public async Task JobCompletedAsync(int userId, int jobId, bool success, string? message = null)
        {
            await NotifyUserAsync(userId, "JobCompleted", new
            {
                JobId = jobId,
                Success = success,
                Message = message
            });
        }

        public async Task SendLogAsync(int runId, string message)
        {
            // Frontend'de .on("ReceiveLog", ...) ile dinleyeceğiz
            await _hub.Clients.Group($"run-{runId}")
                              .SendAsync("ReceiveLog", message);
        }
    }
}
