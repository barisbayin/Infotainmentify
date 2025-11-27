using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/video-assets")]
    public class AutoVideoAssetFilesController : ControllerBase
    {
        private readonly AutoVideoAssetFileService _service;

        public AutoVideoAssetFilesController(AutoVideoAssetFileService service)
        {
            _service = service;
        }

        // -------------------------------------------------------
        // GET ASSETS BY PIPELINE
        // -------------------------------------------------------
        [HttpGet("pipeline/{pipelineId:int}")]
        public async Task<IActionResult> GetByPipeline(int pipelineId, CancellationToken ct)
        {
            var files = await _service.GetByPipelineAsync(pipelineId, ct);
            return Ok(files);
        }


        // -------------------------------------------------------
        // GET SINGLE ASSET
        // -------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var file = await _service.GetAsync(id, ct);
            if (file == null)
                return NotFound();

            return Ok(file);
        }

        // -------------------------------------------------------
        // DELETE ALL FILES OF A PIPELINE (reset scenario)
        // -------------------------------------------------------
        [HttpDelete("pipeline/{pipelineId:int}")]
        public async Task<IActionResult> DeleteByPipeline(int pipelineId, CancellationToken ct)
        {
            var deleted = await _service.DeleteByPipelineAsync(pipelineId, ct);
            return Ok(new { deleted });
        }
    }
}
