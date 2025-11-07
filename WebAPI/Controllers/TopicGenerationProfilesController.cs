using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;
using Application.Contracts.Topics;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TopicGenerationProfilesController : ControllerBase
    {
        private readonly TopicGenerationProfileService _svc;
        private readonly ICurrentUserService _current;

        public TopicGenerationProfilesController(TopicGenerationProfileService svc, ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
        {
            var list = await _svc.ListAsync(_current.UserId, status, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var e = await _svc.GetAsync(_current.UserId, id, ct);
            if (e is null) return NotFound();
            return Ok(e.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TopicGenerationProfileDetailDto dto, CancellationToken ct)
        {
            dto.Id = 0;
            var id = await _svc.UpsertAsync(_current.UserId, dto, ct);
            return Ok(new { Id = id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TopicGenerationProfileDetailDto dto, CancellationToken ct)
        {
            dto.Id = id;
            var updatedId = await _svc.UpsertAsync(_current.UserId, dto, ct);
            return Ok(new { Id = updatedId });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(_current.UserId, id, ct);
            return ok ? Ok() : NotFound();
        }
    }
}
