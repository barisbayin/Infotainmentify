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
    [Route("api/stt-presets")]
    public class SttPresetsController : ControllerBase
    {
        private readonly SttPresetService _service;

        public SttPresetsController(SttPresetService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SttPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);
            return Ok(entities.Select(e => e.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SttPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();
            return Ok(entity.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveSttPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            var entity = new SttPreset
            {
                Name = dto.Name,
                UserAiConnectionId = dto.UserAiConnectionId,
                ModelName = dto.ModelName,
                LanguageCode = dto.LanguageCode,
                EnableWordLevelTimestamps = dto.EnableWordLevelTimestamps,
                EnableSpeakerDiarization = dto.EnableSpeakerDiarization,
                OutputFormat = dto.OutputFormat,
                Prompt = dto.Prompt,
                Temperature = dto.Temperature,
                FilterProfanity = dto.FilterProfanity
            };

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "STT preset created." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveSttPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            entity.Name = dto.Name;
            entity.UserAiConnectionId = dto.UserAiConnectionId;
            entity.ModelName = dto.ModelName;
            entity.LanguageCode = dto.LanguageCode;
            entity.EnableWordLevelTimestamps = dto.EnableWordLevelTimestamps;
            entity.EnableSpeakerDiarization = dto.EnableSpeakerDiarization;
            entity.OutputFormat = dto.OutputFormat;
            entity.Prompt = dto.Prompt;
            entity.Temperature = dto.Temperature;
            entity.FilterProfanity = dto.FilterProfanity;

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
