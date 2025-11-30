using Application.Contracts.AppUser;
using Application.Extensions;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ai-connections")]
    public class AiConnectionsController : ControllerBase
    {
        private readonly UserAiConnectionService _service;

        public AiConnectionsController(UserAiConnectionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _service.ListAsync(User.GetUserId(), ct);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var dto = await _service.GetDetailAsync(id, User.GetUserId(), ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveUserAiConnectionDto dto, CancellationToken ct)
        {
            var id = await _service.CreateAsync(dto, User.GetUserId(), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveUserAiConnectionDto dto, CancellationToken ct)
        {
            await _service.UpdateAsync(id, dto, User.GetUserId(), ct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}
