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
    [Route("api/image-presets")]
    public class ImagePresetsController : ControllerBase
    {
        private readonly ImagePresetService _service;

        public ImagePresetsController(ImagePresetService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImagePresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);
            return Ok(entities.Select(e => e.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ImagePresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);

            if (entity == null) return NotFound();

            return Ok(entity.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveImagePresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            var entity = new ImagePreset
            {
                Name = dto.Name,
                UserAiConnectionId = dto.UserAiConnectionId,
                ModelName = dto.ModelName,
                ArtStyle = dto.ArtStyle,
                Size = dto.Size,
                Quality = dto.Quality,
                PromptTemplate = dto.PromptTemplate,
                NegativePrompt = dto.NegativePrompt,
                ImageCountPerScene = dto.ImageCountPerScene
            };

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "Image preset created." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveImagePresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);

            if (entity == null) return NotFound();

            entity.Name = dto.Name;
            entity.UserAiConnectionId = dto.UserAiConnectionId;
            entity.ModelName = dto.ModelName;
            entity.ArtStyle = dto.ArtStyle;
            entity.Size = dto.Size;
            entity.Quality = dto.Quality;
            entity.PromptTemplate = dto.PromptTemplate;
            entity.NegativePrompt = dto.NegativePrompt;
            entity.ImageCountPerScene = dto.ImageCountPerScene;

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
