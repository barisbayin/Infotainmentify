using Application.Contracts.Mappers;
using Application.Contracts.Prompts;
using Application.Extensions;
using Application.Services;
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

        public PromptsController(PromptService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] bool? active,
            CancellationToken ct)
        {
            var list = await _svc.ListAsync(User.GetUserId(), q, category, active, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            // BaseService metodu (Güvenli Getir)
            var e = await _svc.GetByIdAsync(id, User.GetUserId(), ct);
            return e is null ? NotFound() : Ok(e.ToDetailDto());
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SavePromptDto dto, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(dto, User.GetUserId(), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        // UPDATE
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SavePromptDto dto, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, dto, User.GetUserId(), ct);
            return NoContent();
        }

        // DELETE
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                // BaseService metodu
                await _svc.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException) // veya KeyNotFoundException
            {
                return NotFound();
            }
        }
    }
}
