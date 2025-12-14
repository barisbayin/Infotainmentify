using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebAPI.Hubs
{
    [Authorize]
    public class NotifyHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ [SignalR] User {userId} connected to group user-{userId}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("⚠️ [SignalR] Connection without JWT");
                Console.ResetColor();
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRunGroup(string runId)
        {
            // Güvenlik: İstersen burada "Bu run gerçekten bu user'a mı ait?" kontrolü yapabilirsin.
            // Şimdilik basit tutalım. Group ismini "run-1066" yapıyoruz karışmasın diye.
            string groupName = $"run-{runId}";

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Debug için konsola yazalım
            Console.WriteLine($"✅ User joined log group: {groupName}");
        }

        // Frontend: "Ben sayfadan çıktım, artık izlemiyorum" der.
        public async Task LeaveRunGroup(string runId)
        {
            string groupName = $"run-{runId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}

