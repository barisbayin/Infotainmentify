using Core.Abstractions;
using System.Security.Claims;

namespace WebAPI.Service
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http;
        public CurrentUserService(IHttpContextAccessor http) => _http = http;

        private ClaimsPrincipal? User => _http.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        // JWT'de ClaimTypes.NameIdentifier'a INT yazıyoruz (user.Id.ToString())
        public int UserId
        {
            get
            {
                var val = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(val, out var id) ? id : 0; // 0 => anonim/unknown
            }
        }

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);

        // string tutuyorsan böyle bırak; enum istersen TryParse<UserRole> ile döndürebilirsin
        public string? Role => User?.FindFirstValue(ClaimTypes.Role);

        // İstersen sık kullandığın senaryolar için yardımcı
        public int GetRequiredUserId()
        {
            var id = UserId;
            if (id <= 0) throw new UnauthorizedAccessException("User is not authenticated.");
            return id;
        }
    }
}
