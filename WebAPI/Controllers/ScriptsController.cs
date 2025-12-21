using Application.Contracts.Script;
using Application.Contracts.Story;
using Application.Extensions;
using Application.Mappers;
using Application.Services;
using Core.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptsController : ControllerBase
    {
        private readonly ScriptService _service;

        public ScriptsController(ScriptService svc)
        {
            _service = svc;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScriptListDto>>> List(
                   [FromQuery] int? topicId,
                   [FromQuery] int? conceptId, // 🔥 Eklendi
                   [FromQuery] string? q,
                   CancellationToken ct)
        {
            // Service'e gönderiyoruz
            var list = await _service.ListAsync(User.GetUserId(), topicId, conceptId, q, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var e = await _service.GetByIdAsync(id, User.GetUserId(), ct);
            return e is null ? NotFound() : Ok(e.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveScriptDto dto, CancellationToken ct)
        {
            // Mapping
            var entity = new Script
            {
                TopicId = dto.TopicId,
                Title = dto.Title,
                Content = dto.Content,
                ScenesJson = dto.ScenesJson,
                LanguageCode = dto.LanguageCode,
                EstimatedDurationSec = dto.EstimatedDurationSec,
                Tags = dto.Tags,
                Description = dto.Description
            };

            await _service.AddAsync(entity, User.GetUserId(), ct);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, new { id = entity.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveScriptDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            // Update Map
            entity.TopicId = dto.TopicId;
            entity.Title = dto.Title;
            entity.Content = dto.Content;
            entity.ScenesJson = dto.ScenesJson;
            entity.LanguageCode = dto.LanguageCode;
            entity.EstimatedDurationSec = dto.EstimatedDurationSec;
            entity.Tags = dto.Tags;
            entity.Description = dto.Description;

            await _service.UpdateAsync(entity, userId, ct);
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
            catch (UnauthorizedAccessException) { return NotFound(); }
        }
    }
}
