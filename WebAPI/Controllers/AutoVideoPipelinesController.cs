using Application.Services;
using Core.Abstractions;
using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/video-pipelines")]
    public class AutoVideoPipelinesController : ControllerBase
    {
        private readonly AutoVideoPipelineService _pipelineService;
        private readonly AutoVideoGenerationService _generator;
        private readonly ICurrentUserService _current;

        public AutoVideoPipelinesController(
            AutoVideoPipelineService pipelineService,
            AutoVideoGenerationService generator,
            ICurrentUserService current)
        {
            _pipelineService = pipelineService;
            _generator = generator;
            _current = current;
        }

        // -------------------------------------------------------------
        // GET LIST  (user scoped)
        // -------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _pipelineService.ListAsync(ct);
            return Ok(list);
        }

        // -------------------------------------------------------------
        // GET DETAIL
        // -------------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var p = await _pipelineService.GetAsync(id, ct);
            if (p == null)
                return NotFound();

            return Ok(p);
        }

        // -------------------------------------------------------------
        // GET LOGS
        // -------------------------------------------------------------
        [HttpGet("{id:int}/logs")]
        public async Task<IActionResult> GetLogs(int id, CancellationToken ct)
        {
            var pipeline = await _pipelineService.GetAsync(id, ct);
            if (pipeline == null)
                return NotFound();

            var logs = string.IsNullOrWhiteSpace(pipeline.LogJson)
          ? new List<string>()
          : System.Text.Json.JsonSerializer.Deserialize<List<string>>(pipeline.LogJson!)!;

            return Ok(logs);
        }

        // -------------------------------------------------------------
        // START PIPELINE
        // -------------------------------------------------------------
        [HttpPost("start/{profileId:int}")]
        public async Task<IActionResult> Start(int profileId, CancellationToken ct)
        {
            var pipeline = await _generator.RunAsync(_current.UserId, profileId, ct);
            return Ok(new { pipelineId = pipeline.Id });
        }

        // -------------------------------------------------------------
        // UPDATE STATUS (manual admin / retry ops)
        // -------------------------------------------------------------
        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status, CancellationToken ct)
        {
            if (!Enum.TryParse<ContentPipelineStatus>(status, out var parsed))
                return BadRequest("Geçersiz status.");

            await _pipelineService.UpdateStatusAsync(id, _current.UserId, parsed, ct);
            return Ok();
        }

        // -------------------------------------------------------------
        // DELETE PIPELINE (optional)
        // -------------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            // Direkt repository üzerinden soft delete yoksa ekleyebilirim.
            var p = await _pipelineService.GetAsync(id, ct);
            if (p == null)
                return NotFound();

            p.Removed = true;
            await _pipelineService.UpdateStatusAsync(id, _current.UserId, ContentPipelineStatus.Failed, ct);

            return Ok();
        }

        // -------------------------------------------------------------
        // GET FINAL VIDEO URL
        // -------------------------------------------------------------
        [HttpGet("{id:int}/final-video")]
        public async Task<IActionResult> GetFinalVideo(int id, CancellationToken ct)
        {
            var pipeline = await _pipelineService.GetAsync(id, ct);
            if (pipeline == null || string.IsNullOrWhiteSpace(pipeline.VideoPath))
                return NotFound();

            return Ok(new { path = pipeline.VideoPath });
        }

        // -------------------------------------------------------------
        // GET THUMBNAIL URL
        // -------------------------------------------------------------
        [HttpGet("{id:int}/thumbnail")]
        public async Task<IActionResult> GetThumbnail(int id, CancellationToken ct)
        {
            var pipeline = await _pipelineService.GetAsync(id, ct);
            if (pipeline == null || string.IsNullOrWhiteSpace(pipeline.ThumbnailPath))
                return NotFound();

            return Ok(new { path = pipeline.ThumbnailPath });
        }
    }
}
