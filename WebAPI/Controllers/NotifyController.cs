using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Controllers
{
    public class NotifyController : Controller
    {
        [AllowAnonymous]
        [HttpGet("test-notify")]
        public async Task<IActionResult> TestNotify(
            [FromServices] IHubContext<NotifyHub> hub,
            [FromQuery] int userId = 1)
        {
            await hub.Clients.Group($"user-{userId}")
                .SendAsync("JobProgress", new { jobId = 123, status = "Test", progress = 80 });

            await hub.Clients.Group($"user-{userId}")
                .SendAsync("JobCompleted", new { jobId = 123, success = true, message = "Test tamamlandı!" });

            return Ok("Notification sent.");
        }
    }
}
