using Application.Contracts.UserAiConnection;
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

        public UserAiConnectionsController(UserAiConnectionService svc)
        {
            _svc = svc;
        }

        // GET: api/ai-integrations
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserAiConnectionListDto>>> List(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return Ok(list);
        }

        // GET: api/ai-integrations/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserAiConnectionDetailDto>> Get(int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            if (dto is null)
                return NotFound();
            return Ok(dto);
        }

        // POST: api/ai-integrations
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] UserAiConnectionDetailDto dto, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        // PUT: api/ai-integrations/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserAiConnectionDetailDto dto, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, dto, ct);
            return NoContent();
        }

        // DELETE: api/ai-integrations/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
