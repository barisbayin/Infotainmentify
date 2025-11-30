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
    [Route("api/tts-presets")]
    public class TtsPresetsController : ControllerBase
    {
        private readonly TtsPresetService _service;

        public TtsPresetsController(TtsPresetService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TtsPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);
            return Ok(entities.Select(e => e.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TtsPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();
            return Ok(entity.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveTtsPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = new TtsPreset
            {
                Name = dto.Name,
                UserAiConnectionId = dto.UserAiConnectionId,
                VoiceId = dto.VoiceId,
                LanguageCode = dto.LanguageCode,
                EngineModel = dto.EngineModel,
                SpeakingRate = dto.SpeakingRate,
                Pitch = dto.Pitch,
                Stability = dto.Stability,
                Clarity = dto.Clarity,
                StyleExaggeration = dto.StyleExaggeration
            };

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "TTS preset created." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveTtsPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            entity.Name = dto.Name;
            entity.UserAiConnectionId = dto.UserAiConnectionId;
            entity.VoiceId = dto.VoiceId;
            entity.LanguageCode = dto.LanguageCode;
            entity.EngineModel = dto.EngineModel;
            entity.SpeakingRate = dto.SpeakingRate;
            entity.Pitch = dto.Pitch;
            entity.Stability = dto.Stability;
            entity.Clarity = dto.Clarity;
            entity.StyleExaggeration = dto.StyleExaggeration;

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
