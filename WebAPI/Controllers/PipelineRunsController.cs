using Application.Contracts.Pipeline;
using Application.Extensions;
using Application.Pipeline;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers
{
    [Route("api/pipeline-runs")]
    [ApiController]
    [Authorize]
    public class PipelineRunsController : ControllerBase
    {
        private readonly ContentPipelineRunner _runnerService; // Adı değişmişti, kontrol et
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IRepository<ContentPipelineRun> _runRepo;
        private readonly IUnitOfWork _uow;

        public PipelineRunsController(
            ContentPipelineRunner runnerService,
            IRepository<ContentPipelineTemplate> templateRepo,
            IRepository<ContentPipelineRun> runRepo,
            IUnitOfWork uow)
        {
            _runnerService = runnerService;
            _templateRepo = templateRepo;
            _runRepo = runRepo;
            _uow = uow;
        }

        // ============================================================
        // CREATE (Yeni İş Emri)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateRun([FromBody] CreatePipelineRunRequest request, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // 1. Template kontrolü (Bu kullanıcıya mı ait?)
            var template = await _templateRepo.FirstOrDefaultAsync(
                t => t.Id == request.TemplateId && t.AppUserId == userId,
                asNoTracking: true,
                ct: ct);

            if (template == null)
                return NotFound("Template not found or access denied.");

            // 2. Run kaydını oluştur
            var run = new ContentPipelineRun
            {
                AppUserId = userId,
                TemplateId = request.TemplateId,
                Status = ContentPipelineStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _runRepo.AddAsync(run, ct);
            await _uow.SaveChangesAsync(ct);

            // 3. AutoStart varsa ateşle (Fire-and-Forget)
            if (request.AutoStart)
            {
                // Not: Production'da burası Hangfire/Quartz kuyruğuna atılmalı.
                // Task.Run MVP için OK'dir ama sunucu kapanırsa işlem kaybolur.
                _ = Task.Run(() => _runnerService.RunAsync(run.Id, CancellationToken.None));
            }

            return Ok(new { RunId = run.Id, Message = "Pipeline created." });
        }

        // ============================================================
        // START (Manuel Tetikleme)
        // ============================================================
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRun(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();

            var run = await _runRepo.GetByIdAsync(id, asNoTracking: true, ct);

            if (run == null) return NotFound();
            if (run.AppUserId != userId) return NotFound(); // Güvenlik

            if (run.Status == ContentPipelineStatus.Running)
                return BadRequest("Pipeline is already running.");

            // Arka planda ateşle
            _ = Task.Run(() => _runnerService.RunAsync(run.Id, CancellationToken.None));

            return Ok(new { Message = "Execution started in background." });
        }

        // ============================================================
        // GET DETAILS (Polling / İlerleme Çubuğu)
        // ============================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<PipelineRunDetailDto>> GetRunDetails(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // İlişkileri yükleyerek çekiyoruz
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == id && r.AppUserId == userId,
                include: src => src
                    .Include(r => r.StageExecutions)
                    .ThenInclude(se => se.StageConfig), // Config üzerinden sıralama yapacağız
                asNoTracking: true,
                ct: ct
            );

            if (run == null) return NotFound();

            // DTO Mapping
            var dto = new PipelineRunDetailDto
            {
                Id = run.Id,
                Status = run.Status.ToString(),
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                ErrorMessage = run.ErrorMessage,
                Stages = run.StageExecutions
                    .OrderBy(x => x.StageConfig.Order) // Sıraya diz
                    .Select(s => new PipelineStageDto
                    {
                        StageType = s.StageConfig.StageType.ToString(),
                        Status = s.Status.ToString(),
                        StartedAt = s.StartedAt,
                        FinishedAt = s.FinishedAt,
                        Error = s.Error,
                        DurationMs = s.DurationMs ?? 0
                    }).ToList()
            };

            return Ok(dto);
        }

        // GET api/pipeline-runs
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var runs = await _runRepo.FindAsync(
                predicate: r => r.AppUserId == userId,
                orderBy: r => r.CreatedAt,
                desc: true,
                include: x => x.Include(r => r.Template), // Template adını almak için
                asNoTracking: true,
                ct: ct
            );

            var dtos = runs.Select(r => new
            {
                r.Id,
                TemplateName = r.Template?.Name ?? "Unknown",
                Status = r.Status.ToString(),
                r.StartedAt,
                r.CompletedAt
            });

            return Ok(dtos);
        }
    }
}
