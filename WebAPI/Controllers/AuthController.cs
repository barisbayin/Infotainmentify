using Application.Contracts.Auth;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        public AuthController(AuthService auth) => _auth = auth;

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto req, CancellationToken ct)
        {
            var (user, token) = await _auth.RegisterAsync(req.Email, req.Username, req.Password, ct);

            var resp = new AuthResponseDto
            {
                Token = token,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    DirectoryName = user.DirectoryName
                }
            };
            return Ok(resp);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto req, CancellationToken ct)
        {
            try
            {
                var (user, token) = await _auth.LoginAsync(req.Login, req.Password, ct);

                var resp = new AuthResponseDto
                {
                    Token = token,
                    User = new AuthUserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        Role = user.Role.ToString(),
                        DirectoryName = user.DirectoryName
                    }
                };
                return Ok(resp);
            }
            catch (InvalidOperationException)
            {
                // Invalid credentials
                return Unauthorized(new { message = "Invalid credentials." });
            }
        }
    }
}
