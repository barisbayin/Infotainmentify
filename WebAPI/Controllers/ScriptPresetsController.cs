using Application.Contracts.Presets;
using Application.Extensions;
using Application.Services.PresetService;
using Core.Entity.Presets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers.PresetMappers;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/script-presets")]
    public class ScriptPresetsController : ControllerBase
    {
        private readonly ScriptPresetService _service;

        public ScriptPresetsController(ScriptPresetService service)
        {
            _service = service;
        }

        // GET: List
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScriptPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);
            return Ok(entities.Select(e => e.ToListDto()));
        }

        // GET: Detail
        [HttpGet("{id}")]
        public async Task<ActionResult<ScriptPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);

            if (entity == null) return NotFound();

            return Ok(entity.ToDetailDto());
        }

        // POST: Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveScriptPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            var entity = new ScriptPreset
            {
                Name = dto.Name,
                UserAiConnectionId = dto.UserAiConnectionId,
                ModelName = dto.ModelName,
                Tone = dto.Tone,
                TargetDurationSec = dto.TargetDurationSec,
                Language = dto.Language,
                IncludeHook = dto.IncludeHook,
                IncludeCta = dto.IncludeCta,
                PromptTemplate = dto.PromptTemplate,
                SystemInstruction = dto.SystemInstruction
                // AppUserId serviste set edilir
            };

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "Script preset created." });
        }

        // PUT: Update
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveScriptPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);

            if (entity == null) return NotFound();

            // Update Fields
            entity.Name = dto.Name;
            entity.UserAiConnectionId = dto.UserAiConnectionId;
            entity.ModelName = dto.ModelName;
            entity.Tone = dto.Tone;
            entity.TargetDurationSec = dto.TargetDurationSec;
            entity.Language = dto.Language;
            entity.IncludeHook = dto.IncludeHook;
            entity.IncludeCta = dto.IncludeCta;
            entity.PromptTemplate = dto.PromptTemplate;
            entity.SystemInstruction = dto.SystemInstruction;

            await _service.UpdateAsync(entity, userId, ct);
            return NoContent();
        }

        // DELETE
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
