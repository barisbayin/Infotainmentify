using Application.Contracts.Render;
using Application.Extensions;
using Application.Mappers;
using Application.Mappers.PresetMappers;
using Application.Services.PresetService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/render-presets")]
    public class RenderPresetsController : ControllerBase
    {
        private readonly RenderPresetService _service;

        public RenderPresetsController(RenderPresetService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RenderPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);

            // Mapper Extension Kullanımı: .ToListDto()
            return Ok(entities.Select(e => e.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RenderPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            // Mapper Extension Kullanımı: .ToDetailDto()
            return Ok(entity.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveRenderPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // 🔥 DTO -> Entity Dönüşümü (TEK SATIR)
            var entity = dto.ToEntity();

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "Preset created." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveRenderPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            // 🔥 Entity Güncelleme (TEK SATIR)
            entity.UpdateEntity(dto);

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
