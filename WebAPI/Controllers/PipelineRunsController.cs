using Application.Contracts.Pipeline;
using Application.Extensions;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/pipeline-runs")]
    [ApiController]
    [Authorize]
    public class PipelineRunsController : ControllerBase
    {
        private readonly IContentPipelineService _pipelineService;

        public PipelineRunsController(IContentPipelineService pipelineService)
        {
            _pipelineService = pipelineService;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> CreateRun([FromBody] CreatePipelineRunRequest request, CancellationToken ct)
        {
            try
            {
                int userId = User.GetUserId();
                int runId = await _pipelineService.CreateRunAsync(userId, request, ct);
                return Ok(new { RunId = runId, Message = "Pipeline created." });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // START
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRun(int id, CancellationToken ct)
        {
            try
            {
                int userId = User.GetUserId();
                await _pipelineService.StartRunAsync(userId, id, ct);
                return Ok(new { Message = "Execution started." });
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        // GET DETAILS
        [HttpGet("{id}")]
        public async Task<ActionResult<PipelineRunDetailDto>> GetRunDetails(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var result = await _pipelineService.GetRunDetailsAsync(userId, id, ct);

            if (result == null) return NotFound();
            return Ok(result);
        }

        // LIST
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PipelineRunListDto>>> List([FromQuery] int? conceptId, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var results = await _pipelineService.ListRunsAsync(userId, conceptId, ct);
            return Ok(results);
        }

        // RETRY
        [HttpPost("{id}/retry/{stageType}")]
        public async Task<IActionResult> RetryStage(int id, string stageType, CancellationToken ct)
        {
            try
            {
                int userId = User.GetUserId();
                await _pipelineService.RetryStageAsync(userId, id, stageType, ct);
                return Ok(new { message = $"{stageType} tekrar kuyruğa alındı." });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        // LOGS
        [HttpGet("{id}/logs")]
        public async Task<IActionResult> GetRunLogs(int id)
        {
            // Loglar genelde herkese açıksa ID yeterli, kullanıcı kontrolü servis içinde de yapılabilir ama log okuma genelde güvenlidir.
            var logs = await _pipelineService.GetRunLogsAsync(id);
            if (logs == null || !logs.Any()) return Ok(new List<string>()); // Boş liste dön

            return Ok(logs);
        }
    }
}
