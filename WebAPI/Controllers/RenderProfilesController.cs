using Application.Contracts.Render;
using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RenderProfilesController : ControllerBase
    {
        private readonly RenderProfileService _svc;
        private readonly ICurrentUserService _current;

        public RenderProfilesController(
            RenderProfileService svc,
            ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _svc.ListAsync(_current.UserId, ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(_current.UserId, id, ct);
            return item == null
                ? NotFound(new { message = "RenderProfile bulunamadı." })
                : Ok(item);
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] RenderProfileDetailDto dto, CancellationToken ct)
        {
            try
            {
                var id = await _svc.UpsertAsync(_current.UserId, dto, ct);
                return Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(_current.UserId, id, ct);

            return ok
                ? Ok(new { success = true })
                : NotFound(new { message = "RenderProfile bulunamadı." });
        }
    }
}
