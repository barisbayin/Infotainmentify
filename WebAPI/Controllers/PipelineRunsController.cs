using Application.Contracts.Pipeline;
using Application.Extensions;
using Application.Models;
using Application.Services.Interfaces;
using Core.Enums;
using Google.Api;
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
        // 1. Basit Retry (Hata alınca tekrar dene butonu)
        [HttpPost("retry/{runId}/{stageType}")]
        public async Task<IActionResult> Retry(int runId, string stageType, CancellationToken ct)
        {
            // newPresetId yok, sadece tekrar dene
            await _pipelineService.RetryStageAsync(User.GetUserId(), runId, stageType, null, ct);
            return Ok(new { message = "Retry işlemi başlatıldı." });
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


        // 2. Re-Render (Yeni ayarla tekrar oluştur butonu)
        [HttpPost("re-render")]
        public async Task<IActionResult> ReRender([FromBody] ReRenderRequest request, CancellationToken ct)
        {
            // newPresetId VAR, render'ı yeni ayarla baştan yap
            await _pipelineService.RetryStageAsync(
                User.GetUserId(),
                request.RunId,
                "Render", // Veya StageType.Render.ToString()
                request.NewRenderPresetId,
                ct
            );
            return Ok(new { message = "Re-render işlemi başlatıldı." });
        }


        [HttpPost("{runId}/approve")]
        public async Task<IActionResult> ApproveRun(int runId, CancellationToken ct)
        {
            try
            {
                // Tüm işi servise yıktık
                await _pipelineService.ApproveRunAsync(runId, ct);

                return Ok(new { message = "🚀 Onay verildi, süreç kaldığı yerden devam ediyor!" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Pipeline Run bulunamadı.");
            }
            catch (InvalidOperationException ex)
            {
                // "Onay beklemiyor" hatası buradan döner
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
            }
        }
    }
}
