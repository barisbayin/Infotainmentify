using System.Security.Claims;

namespace Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            // JWT içindeki "sub" veya "nameid" claim'ini arar
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value;

            if (int.TryParse(idClaim, out var id))
            {
                return id;
            }

            // Eğer ID yoksa veya parse edilemezse 0 dön veya hata fırlat
            // throw new UnauthorizedAccessException("User ID not found in token.");
            return 0;
        }
    }
}
