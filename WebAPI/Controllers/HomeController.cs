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
    public class ScriptsController : ControllerBase
    {
        private readonly ScriptService _svc;
        private readonly ICurrentUserService _current;

        public ScriptsController(ScriptService svc, ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        // ---------------- LIST ----------------
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int? topicId,
            [FromQuery] string? q,
            CancellationToken ct)
        {
            var list = await _svc.ListAsync(topicId, q, ct);
            return Ok(list);
        }

        // ---------------- GET ----------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(id, ct);
            if (item == null)
                return NotFound(new { message = "Script bulunamadı." });

            return Ok(item);
        }

        // ---------------- CREATE ----------------
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] ScriptDetailsDto dto,
            CancellationToken ct)
        {
            var created = await _svc.CreateAsync(dto, ct);
            return Ok(created);
        }

        // ---------------- UPDATE ----------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ScriptDetailsDto dto,
            CancellationToken ct)
        {
            var updated = await _svc.UpdateAsync(id, dto, ct);
            return Ok(updated);
        }

        // ---------------- DELETE ----------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return Ok(new { success = true });
        }
    }
}
