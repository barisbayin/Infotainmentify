using Application.Contracts.Mappers;
using Application.Contracts.Prompts;
using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PromptsController : ControllerBase
    {
        private readonly PromptService _svc;
        private readonly ICurrentUserService _current;

        public PromptsController(PromptService svc, ICurrentUserService current)
        { _svc = svc; _current = current; }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] string? category, [FromQuery] bool? active, CancellationToken ct)
        {
            var list = await _svc.ListAsync(_current.UserId, q, category, active, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var e = await _svc.GetAsync(_current.UserId, id, ct);
            return e is null ? NotFound() : Ok(e.ToDetailDto());
        }

        // Tek endpoint: create/update (Id==0 => create)
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] PromptDetailDto dto, CancellationToken ct)
        {
            var id = await _svc.UpsertAsync(_current.UserId, dto, ct);
            if (dto.Id == 0)
                return CreatedAtAction(nameof(Get), new { id }, new { id });
            return Ok(new { id });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => (await _svc.DeleteAsync(_current.UserId, id, ct)) ? NoContent() : NotFound();
    }
}
