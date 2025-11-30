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
        private readonly ScriptService _svc;

        public ScriptsController(ScriptService svc)
        {
            _svc = svc;
        }

        // GET: api/scripts?topicId=5&q=hello
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScriptListDto>>> List(
            [FromQuery] int? topicId,
            [FromQuery] string? q,
            CancellationToken ct)
        {
            var list = await _svc.ListAsync(User.GetUserId(), topicId, q, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScriptDetailDto>> Get(int id, CancellationToken ct)
        {
            var e = await _svc.GetByIdAsync(id, User.GetUserId(), ct);
            return e is null ? NotFound() : Ok(e.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveScriptDto dto, CancellationToken ct)
        {
            var entity = new Script
            {
                TopicId = dto.TopicId,
                Title = dto.Title,
                Content = dto.Content,
                ScenesJson = dto.ScenesJson,
                LanguageCode = dto.LanguageCode,
                EstimatedDurationSec = dto.EstimatedDurationSec
                // AppUserId service içinde set edilir
            };

            await _svc.AddAsync(entity, User.GetUserId(), ct);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, new { id = entity.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveScriptDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var entity = await _svc.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            // Update
            entity.TopicId = dto.TopicId; // Topic değişebilir mi? Evet.
            entity.Title = dto.Title;
            entity.Content = dto.Content;
            entity.ScenesJson = dto.ScenesJson;
            entity.LanguageCode = dto.LanguageCode;
            entity.EstimatedDurationSec = dto.EstimatedDurationSec;

            await _svc.UpdateAsync(entity, userId, ct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _svc.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}
