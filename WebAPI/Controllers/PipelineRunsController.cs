using Application.Services;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PipelineRunsController : ControllerBase
    {
        private readonly PipelineRunnerService _runnerService;
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IRepository<ContentPipelineRun> _runRepo;
        private readonly IUnitOfWork _uow;

        public PipelineRunsController(
            PipelineRunnerService runnerService,
            IRepository<ContentPipelineTemplate> templateRepo,
            IRepository<ContentPipelineRun> runRepo,
            IUnitOfWork uow)
        {
            _runnerService = runnerService;
            _templateRepo = templateRepo;
            _runRepo = runRepo;
            _uow = uow;
        }

        // POST api/pipelineruns
        // Yeni bir iş emri oluşturur
        [HttpPost]
        public async Task<IActionResult> CreateRun([FromBody] CreatePipelineRunRequest request)
        {
            // 1. Kullanıcıyı tanı (JWT'den)
            // int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            int userId = 1; // Şimdilik hardcode, auth entegrasyonuna göre açarsın

            // 2. Template var mı ve bu kullanıcıya mı ait?
            var template = await _templateRepo.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.AppUserId == userId);
            if (template == null) return NotFound("Template not found or access denied.");

            // 3. Run kaydını oluştur (Pending)
            var run = new ContentPipelineRun
            {
                AppUserId = userId,
                TemplateId = request.TemplateId,
                Status = ContentPipelineStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _runRepo.AddAsync(run);
            await _uow.SaveChangesAsync();

            // 4. Eğer AutoStart istenmişse tetiği çek!
            if (request.AutoStart)
            {
                // 🔥 KRİTİK NOKTA: Arka planda başlat, cevabı bekleme!
                // Gerçek bir projede burası Hangfire veya Quartz kuyruğuna atılmalı.
                // Şimdilik Task.Run ile thread havuzuna atıyoruz (Basit MVP).
                _ = Task.Run(() => _runnerService.ExecuteRunAsync(run.Id));
            }

            return Ok(new { RunId = run.Id, Message = "Pipeline created and started." });
        }

        // POST api/pipelineruns/{id}/start
        // Daha önce oluşturulmuş ama çalışmamış veya durmuş bir run'ı tetikler
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartRun(int id)
        {
            // int userId = ...
            int userId = 1;

            var run = await _runRepo.GetByIdAsync(id);
            if (run == null || run.AppUserId != userId) return NotFound();

            if (run.Status == ContentPipelineStatus.Running)
                return BadRequest("Pipeline is already running.");

            // Arka planda ateşle
            _ = Task.Run(() => _runnerService.ExecuteRunAsync(run.Id));

            return Ok(new { Message = "Execution started in background." });
        }

        // GET api/pipelineruns/{id}
        // Frontend buradan sürekli (polling) durum soracak
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRunDetails(int id)
        {
            // Detaylı çekiyoruz (Stage'leri görmek için)
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == id,
                include: src => src.Include(r => r.StageExecutions).ThenInclude(se => se.StageConfig)
            );

            if (run == null) return NotFound();

            // Frontend için sade bir DTO dönüyoruz
            var result = new
            {
                run.Id,
                Status = run.Status.ToString(),
                run.StartedAt,
                run.CompletedAt,
                run.ErrorMessage,
                Stages = run.StageExecutions.OrderBy(x => x.StageConfig.Order).Select(s => new
                {
                    s.StageConfig.StageType,
                    Status = s.Status.ToString(),
                    s.StartedAt,
                    s.FinishedAt,
                    s.Error,
                    // OutputJson'ı frontend'e ham string olarak değil, nesne olarak yollamak istersen:
                    // Output = string.IsNullOrEmpty(s.OutputJson) ? null : JsonSerializer.Deserialize<object>(s.OutputJson)
                })
            };

            return Ok(result);
        }
    }
}
