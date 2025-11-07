using Application.Contracts.Topics;
using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TopicsController : ControllerBase
    {
        private readonly TopicService _svc;
        private readonly ICurrentUserService _current;

        public TopicsController(TopicService svc, ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        // ------------------ LIST ------------------
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? q,
            [FromQuery] string? category,
            CancellationToken ct)
        {
            var list = await _svc.ListAsync(_current.UserId, q, category, ct);
            return Ok(list);
        }

        // ------------------ GET ------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(_current.UserId, id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        // ------------------ CREATE/UPDATE ------------------
        /// <summary>
        /// Id == 0 → Create, Id > 0 → Update
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] TopicDetailDto dto, CancellationToken ct)
        {
            if (dto == null)
                return BadRequest("Geçersiz istek verisi.");

            var id = await _svc.UpsertAsync(_current.UserId, dto, ct);

            return dto.Id == 0
                ? CreatedAtAction(nameof(Get), new { id }, new { id })
                : Ok(new { id });
        }

        // ------------------ DELETE ------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var success = await _svc.DeleteAsync(_current.UserId, id, ct);
            return success ? NoContent() : NotFound();
        }

        // ------------------ TOGGLE ACTIVE ------------------
        [HttpPut("{id:int}/active")]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] bool isActive, CancellationToken ct)
        {
            await _svc.ToggleActiveAsync(_current.UserId, id, isActive, ct);
            return Ok(new { id, isActive });
        }
    }
}
