using Application.Contracts.Script;
using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptGenerationProfilesController : ControllerBase
    {
        private readonly ScriptGenerationProfileService _svc;
        private readonly ICurrentUserService _current;

        public ScriptGenerationProfilesController(
            ScriptGenerationProfileService svc,
            ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        // ---------------- LIST ----------------
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
        {
            var list = await _svc.ListAsync(_current.UserId, status, ct);
            return Ok(list);
        }

        // ---------------- GET ----------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(_current.UserId, id, ct);
            if (item == null)
                return NotFound(new { message = "ScriptGenerationProfile bulunamadı." });

            return Ok(item);
        }

        // ---------------- UPSERT ----------------
        [HttpPost("save")] // veya sadece [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] ScriptGenerationProfileDetailDto dto, CancellationToken ct)
        {
            if (dto == null)
                return BadRequest(new { message = "Veri alınamadı." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        // ---------------- DELETE ----------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(_current.UserId, id, ct);
            if (!ok)
                return NotFound(new { message = "ScriptGenerationProfile bulunamadı." });

            return Ok(new { success = true });
        }
    }
}
