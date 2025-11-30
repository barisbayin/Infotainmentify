using Application.Contracts.Pipeline;
using Application.Extensions;
using Application.Services.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers;

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
        public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
        {
            var list = await _service.ListAsync(User.GetUserId(), q, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var entity = await _service.GetByIdAsync(id, User.GetUserId(), ct);
            return entity is null ? NotFound() : Ok(entity.ToDetailDto());
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
