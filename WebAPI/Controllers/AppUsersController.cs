using Application.Contracts.AppUser;
using Application.Services;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/app-users")]
    public class AppUsersController : ControllerBase
    {
        private readonly AppUserService _svc;
        public AppUsersController(AppUserService svc) => _svc = svc;

        // everyone (auth): kendi FE’in için basit liste
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AppUserListDto>>> List(CancellationToken ct)
            => Ok(await _svc.ListAsync(ct));

        // detail
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppUserDetailDto>> Get(int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        // me
        [HttpGet("me")]
        public async Task<ActionResult<AppUserDetailDto>> Me(CancellationToken ct)
            => Ok(await _svc.MeAsync(ct));

        // me update (body: { email, username })
        public sealed record UpdateMeRequest(string Email, string Username);

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest req, CancellationToken ct)
        {
            await _svc.UpdateMeAsync(req.Email, req.Username, ct);
            return NoContent();
        }

        // me change password (body: { currentPassword, newPassword })
        public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
        {
            await _svc.ChangePasswordAsync(req.CurrentPassword, req.NewPassword, ct);
            return NoContent();
        }

        // --- admin mini set ---
        public sealed record SetRoleRequest(string Role); // "Admin" | "Normal"

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/role")]
        public async Task<IActionResult> SetRole(int id, [FromBody] SetRoleRequest req, CancellationToken ct)
        {
            if (!Enum.TryParse<UserRole>(req.Role, true, out var role))
                return BadRequest(new { message = "Role must be 'Admin' or 'Normal'." });

            await _svc.SetRoleAsync(id, role, ct);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool active, CancellationToken ct)
        {
            await _svc.SetActiveAsync(id, active, ct);
            return NoContent();
        }
    }
}

