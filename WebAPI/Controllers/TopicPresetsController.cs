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
    [Route("api/topic-presets")]
    public class TopicPresetsController : ControllerBase
    {
        private readonly TopicPresetService _service;

        public TopicPresetsController(TopicPresetService service)
        {
            _service = service;
        }

        // =================================================================
        // LIST (Grid İçin Hafif Veri)
        // =================================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TopicPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);

            // Extension method ile temiz dönüşüm
            var dtos = entities.Select(e => e.ToListDto());

            return Ok(dtos);
        }

        // =================================================================
        // DETAIL (Form Doldurmak İçin Ağır Veri)
        // =================================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<TopicPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);

            if (entity == null) return NotFound();

            // Entity -> DetailDto
            return Ok(entity.ToDetailDto());
        }

        // =================================================================
        // CREATE
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveTopicPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // Mapping (Manuel - SaveDto -> Entity)
            var entity = new TopicPreset
            {
                Name = dto.Name,
                Description = dto.Description,
                UserAiConnectionId = dto.UserAiConnectionId,
                ModelName = dto.ModelName,
                Temperature = dto.Temperature,
                Language = dto.Language,
                PromptTemplate = dto.PromptTemplate,
                ContextKeywordsJson = dto.ContextKeywordsJson,
                SystemInstruction = dto.SystemInstruction
                // AppUserId, Service içindeki Base metodda set ediliyor
            };

            await _service.AddAsync(entity, userId, ct);

            return Ok(new { id = entity.Id, message = "Preset created." });
        }

        // =================================================================
        // UPDATE (Eksikti, Ekledik)
        // =================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveTopicPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // 1. Mevcut kaydı çek (Yetki kontrolü BaseService'de yapılır)
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            // 2. Güncelle
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.UserAiConnectionId = dto.UserAiConnectionId;
            entity.ModelName = dto.ModelName;
            entity.Temperature = dto.Temperature;
            entity.Language = dto.Language;
            entity.PromptTemplate = dto.PromptTemplate;
            entity.ContextKeywordsJson = dto.ContextKeywordsJson;
            entity.SystemInstruction = dto.SystemInstruction;

            // 3. Kaydet
            await _service.UpdateAsync(entity, userId, ct);

            return NoContent();
        }

        // =================================================================
        // DELETE
        // =================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();

            try
            {
                await _service.DeleteAsync(id, userId, ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}
