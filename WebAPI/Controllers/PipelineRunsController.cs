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
        private readonly IServiceProvider _sp; // 🔥 YENİ EKLENDİ
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IRepository<ContentPipelineRun> _runRepo;
        private readonly IUnitOfWork _uow;

        public PipelineRunsController(
            IServiceProvider sp, // 🔥 YENİ EKLENDİ
            IRepository<ContentPipelineTemplate> templateRepo,
            IRepository<ContentPipelineRun> runRepo,
            IUnitOfWork uow)
        {
            _sp = sp;
            _templateRepo = templateRepo;
            _runRepo = runRepo;
            _uow = uow;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> CreateRun([FromBody] CreatePipelineRunRequest request, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // ... (Template kontrolü ve Run kaydı aynı) ...
            var template = await _templateRepo.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.AppUserId == userId, asNoTracking: true, ct: ct);
            if (template == null) return NotFound("Template not found.");

            var run = new ContentPipelineRun
            {
                AppUserId = userId,
                TemplateId = request.TemplateId,
                Status = ContentPipelineStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _runRepo.AddAsync(run, ct);
            await _uow.SaveChangesAsync(ct);

            if (request.AutoStart)
            {
                // 🔥 FIX: Scope Oluşturma
                // Arka plan işlemi olduğu için yeni bir scope lazım.
                _ = Task.Run(async () =>
                {
                    using var scope = _sp.CreateScope();
                    var runner = scope.ServiceProvider.GetRequiredService<ContentPipelineRunner>();
                    try
                    {
                        await runner.RunAsync(run.Id, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BACKGROUND ERROR] Run #{run.Id}: {ex}");
                        // Hata loglaması buraya
                    }
                });
            }

            return Ok(new { RunId = run.Id, Message = "Pipeline created and started." });
        }

        // START
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRun(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var run = await _runRepo.GetByIdAsync(id, asNoTracking: true, ct);

            if (run == null || run.AppUserId != userId) return NotFound();
            if (run.Status == ContentPipelineStatus.Running) return BadRequest("Already running.");

            // 🔥 FIX: Scope Oluşturma
            _ = Task.Run(async () =>
            {
                using var scope = _sp.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<ContentPipelineRunner>();
                await runner.RunAsync(run.Id, CancellationToken.None);
            });

            return Ok(new { Message = "Execution started." });
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
                        DurationMs = s.DurationMs ?? 0,
                        OutputJson = s.OutputJson,
                    }).ToList()
            };

            return Ok(dto);
        }

        // GET api/pipeline-runs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PipelineRunListDto>>> List(
                  [FromQuery] int? conceptId, // 🔥 YENİ
                  CancellationToken ct)
        {
            int userId = User.GetUserId();

            var runs = await _runRepo.FindAsync(
                predicate: r =>
                    r.AppUserId == userId &&
                    // 🔥 KRİTİK: İlişki üzerinden filtreleme
                    (!conceptId.HasValue || r.Template.ConceptId == conceptId),
                orderBy: r => r.CreatedAt,
                desc: true,
                include: x => x.Include(r => r.Template),
                asNoTracking: true,
                ct: ct
            );

            // 🔥 DÜZELTME: Anonymous Object yerine DTO kullandık
            var dtos = runs.Select(r => new PipelineRunListDto
            {
                Id = r.Id,
                RunContextTitle = r.RunContextTitle,
                TemplateName = r.Template?.Name ?? "Silinmiş Şablon", // Null check
                Status = r.Status.ToString(),
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt
            });

            return Ok(dtos);
        }
    }
}
