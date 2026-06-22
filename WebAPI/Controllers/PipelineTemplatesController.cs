using Application.Contracts.Pipeline;
using Application.Extensions;
using Application.Services.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;
using System.Text.Json;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/pipeline-templates")]
    public class PipelineTemplatesController : ControllerBase
    {
        private readonly PipelineTemplateService _service;

        public PipelineTemplatesController(PipelineTemplateService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> List(
                    [FromQuery] string? q,
                    [FromQuery] int? conceptId, // 🔥 YENİ
                    CancellationToken ct)
        {
            var list = await _service.ListAsync(User.GetUserId(), q, conceptId, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var entity = await _service.GetByIdAsync(id, User.GetUserId(), ct);
            return entity is null ? NotFound() : Ok(entity.ToDetailDto());
        }

        [HttpGet("{id}/health")]
        public async Task<IActionResult> GetHealth(int id, CancellationToken ct)
        {
            var health = await _service.GetHealthAsync(id, User.GetUserId(), ct);
            return health is null ? NotFound() : Ok(health);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SavePipelineTemplateDto dto, CancellationToken ct)
        {
            var id = await _service.CreateAsync(dto, User.GetUserId(), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SavePipelineTemplateDto dto, CancellationToken ct)
        {
            await _service.UpdateAsync(id, dto, User.GetUserId(), ct);
            return NoContent();
        }

        [HttpPut("{id}/workflow-layout")]
        public async Task<IActionResult> UpdateWorkflowLayout(int id, [FromBody] UpdateWorkflowLayoutDto dto, CancellationToken ct)
        {
            try
            {
                await _service.UpdateWorkflowLayoutAsync(id, dto, User.GetUserId(), ct);
                return NoContent();
            }
            catch (JsonException)
            {
                return BadRequest("Workflow layout JSON gecersiz.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
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
