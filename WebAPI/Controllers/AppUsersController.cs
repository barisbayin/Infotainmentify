using Application.Contracts.AppUser;
using Application.Services;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize] // Varsayılan: Giriş yapmış herkes
    [ApiController]
    [Route("api/app-users")]
    public class AppUsersController : ControllerBase
    {
        private readonly AppUserService _svc;
        public AppUsersController(AppUserService svc) => _svc = svc;

        // =================================================================
        // ADMIN ENDPOINTS (🔒 Kilitlendi)
        // =================================================================

        // Listeleme: Sadece Admin diğer kullanıcıları görebilir.
        [HttpGet]
        [Authorize(Roles = "Admin")] // <--- EKLEME
        public async Task<ActionResult<IReadOnlyList<AppUserListDto>>> List(CancellationToken ct)
            => Ok(await _svc.ListAsync(ct));

        // Detay: Sadece Admin başkasının ID'si ile detay çekebilir.
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")] // <--- EKLEME
        public async Task<ActionResult<AppUserDetailDto>> Get(int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        // --- Admin İşlemleri ---

        // Request DTO'larını controller içinde record olarak tutman PRATİK ama
        // proje büyürse bunları 'Application.Contracts' altına taşıman daha temiz olur.
        public sealed record SetRoleRequest(string Role);

        [HttpPost("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetRole(int id, [FromBody] SetRoleRequest req, CancellationToken ct)
        {
            if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
                return BadRequest(new { message = "Role must be 'Admin' or 'Normal'." });

            await _svc.SetRoleAsync(id, role, ct);
            return NoContent();
        }

        [HttpPost("{id:int}/active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool active, CancellationToken ct)
        {
            await _svc.SetActiveAsync(id, active, ct);
            return NoContent();
        }

        // =================================================================
        // PERSONAL ENDPOINTS (Herkes Kendine)
        // =================================================================

        // Ben Kimim?
        [HttpGet("me")]
        public async Task<ActionResult<AppUserDetailDto>> Me(CancellationToken ct)
            => Ok(await _svc.MeAsync(ct));

        // Profilimi Güncelle
        public sealed record UpdateMeRequest(string Email, string Username);

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest req, CancellationToken ct)
        {
            await _svc.UpdateMeAsync(req.Email, req.Username, ct);
            return NoContent();
        }

        // Şifremi Değiştir
        public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            await _svc.ChangePasswordAsync(req.CurrentPassword, req.NewPassword, ct);
            return NoContent();
        }
    }
}


