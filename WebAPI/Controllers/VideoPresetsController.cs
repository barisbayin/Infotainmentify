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
    [Route("api/video-presets")]
    public class VideoPresetsController : ControllerBase
    {
        private readonly VideoPresetService _service;

        public VideoPresetsController(VideoPresetService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VideoPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);
            return Ok(entities.Select(e => e.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VideoPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            return entity is null ? NotFound() : Ok(entity.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveVideoPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = new VideoPreset
            {
                Name = dto.Name,
                UserAiConnectionId = dto.UserAiConnectionId,
                ModelName = dto.ModelName,
                GenerationMode = dto.GenerationMode,
                AspectRatio = dto.AspectRatio,
                DurationSeconds = dto.DurationSeconds,
                PromptTemplate = dto.PromptTemplate,
                NegativePrompt = dto.NegativePrompt,
                CameraControlSettingsJson = dto.CameraControlSettingsJson,
                AdvancedSettingsJson = dto.AdvancedSettingsJson
            };

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "Video preset created." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveVideoPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            entity.Name = dto.Name;
            entity.UserAiConnectionId = dto.UserAiConnectionId;
            entity.ModelName = dto.ModelName;
            entity.GenerationMode = dto.GenerationMode;
            entity.AspectRatio = dto.AspectRatio;
            entity.DurationSeconds = dto.DurationSeconds;
            entity.PromptTemplate = dto.PromptTemplate;
            entity.NegativePrompt = dto.NegativePrompt;
            entity.CameraControlSettingsJson = dto.CameraControlSettingsJson;
            entity.AdvancedSettingsJson = dto.AdvancedSettingsJson;

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
