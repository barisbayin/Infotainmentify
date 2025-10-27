using Application.Contracts.Ai;
using Application.Contracts.Enums;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ai-integrations")]
    public class UserAiConnectionsController : ControllerBase
    {
        private readonly UserAiConnectionService _svc;

        public UserAiConnectionsController(UserAiConnectionService svc) => _svc = svc;

        // GET: api/ai-integrations/{id}
        // ?exposure=masked|plain
        public record GetDetailQuery(string exposure = "masked");
        public record PlainRequestBody(string? currentPassword);

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserAiConnectionDetailDto>> Get(
            int id,
            [FromQuery] GetDetailQuery q,
            [FromBody] PlainRequestBody? body, // plain için currentPassword
            CancellationToken ct = default)
        {
            var exposure = q.exposure?.ToLowerInvariant() switch
            {
                "plain" => CredentialExposure.Plain,
                "masked" => CredentialExposure.Masked,
                _ => CredentialExposure.Masked
            };

            var dto = await _svc.GetAsync(id, exposure, body?.currentPassword, ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }
    }
}
