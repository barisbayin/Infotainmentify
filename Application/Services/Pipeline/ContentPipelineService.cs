using Application.Abstractions;
using Application.Contracts.Pipeline;
using Application.Models;
using Application.Services.Interfaces;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Application.Services.Pipeline
{
    public class ContentPipelineService : IContentPipelineService
    {
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IRepository<ContentPipelineRun> _runRepo;
        private readonly IUnitOfWork _uow;
        private readonly IServiceProvider _sp; // Background job için
        private readonly IContentPipelineRunner _runner; // Retry için

        public ContentPipelineService(
            IRepository<ContentPipelineTemplate> templateRepo,
            IRepository<ContentPipelineRun> runRepo,
            IUnitOfWork uow,
            IServiceProvider sp,
            IContentPipelineRunner runner)
        {
            _templateRepo = templateRepo;
            _runRepo = runRepo;
            _uow = uow;
            _sp = sp;
            _runner = runner;
        }

        public async Task<int> CreateRunAsync(int userId, CreatePipelineRunRequest request, CancellationToken ct)
        {
            var template = await _templateRepo.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.AppUserId == userId, asNoTracking: true, ct: ct);
            if (template == null) throw new KeyNotFoundException("Template bulunamadı.");

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
                FireAndForgetRun(run.Id);
            }

            return run.Id;
        }

        public async Task StartRunAsync(int userId, int runId, CancellationToken ct)
        {
            var run = await _runRepo.GetByIdAsync(runId, asNoTracking: true, ct);
            if (run == null || run.AppUserId != userId) throw new KeyNotFoundException("Run bulunamadı.");
            if (run.Status == ContentPipelineStatus.Running) throw new InvalidOperationException("Run zaten çalışıyor.");

            FireAndForgetRun(run.Id);
        }

        public async Task<PipelineRunDetailDto?> GetRunDetailsAsync(int userId, int runId, CancellationToken ct)
        {
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId && r.AppUserId == userId,
                include: src => src
                    .Include(r => r.StageExecutions)
                    .ThenInclude(se => se.StageConfig),
                asNoTracking: true,
                ct: ct
            );

            if (run == null) return null;

            // 🔥 VİDEO URL'İNİ BULMA MANTIĞI
            string? videoUrl = null;

            // 1. Render aşamasını bul
            var renderExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == StageType.Render);

            // 2. Eğer bitmişse ve çıktısı varsa JSON'u parse et
            if (renderExec != null && renderExec.Status == StageStatus.Completed && !string.IsNullOrEmpty(renderExec.OutputJson))
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<RenderStagePayload>(renderExec.OutputJson);
                    videoUrl = payload?.VideoUrl; // URL'i kaptık!
                }
                catch { /* JSON bozuksa yapacak bir şey yok, null kalsın */ }
            }

            return new PipelineRunDetailDto
            {
                Id = run.Id,
                Status = run.Status.ToString(),
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                ErrorMessage = run.ErrorMessage,
                FinalVideoUrl = videoUrl,
                Stages = run.StageExecutions.OrderBy(x => x.StageConfig.Order).Select(s => new PipelineStageDto
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
        }

        public async Task<IEnumerable<PipelineRunListDto>> ListRunsAsync(int userId, int? conceptId, CancellationToken ct)
        {
            var runs = await _runRepo.FindAsync(
                predicate: r => r.AppUserId == userId && (!conceptId.HasValue || r.Template.ConceptId == conceptId),
                orderBy: r => r.CreatedAt,
                desc: true,
                include: x => x.Include(r => r.Template),
                asNoTracking: true,
                ct: ct
            );

            return runs.Select(r => new PipelineRunListDto
            {
                Id = r.Id,
                RunContextTitle = r.RunContextTitle,
                TemplateName = r.Template?.Name ?? "Silinmiş Şablon",
                Status = r.Status.ToString(),
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt
            });
        }

        public async Task RetryStageAsync(int userId, int runId, string stageType, int? newPresetId = null, CancellationToken ct = default)
        {
            // 1. Güvenlik Kontrolü (Run bu user'a mı ait?)
            // GetByIdAsync muhtemelen sadece ID ile çekiyor, userId kontrolünü aşağıda yapıyoruz.
            var run = await _runRepo.GetByIdAsync(runId, false, ct);

            if (run == null || run.AppUserId != userId)
                throw new KeyNotFoundException("Run bulunamadı veya erişim yetkiniz yok.");

            // 2. Runner'a devret (newPresetId parametresini de paslıyoruz)
            await _runner.RetryStageAsync(runId, stageType, newPresetId, ct);
        }

        public async Task<List<string>> GetRunLogsAsync(int runId)
        {
            // 🔥 DÜZELTME: GetByIdAsync yerine FirstOrDefaultAsync kullanıyoruz
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId,
                include: source => source
                    .Include(r => r.StageExecutions)
                    .ThenInclude(s => s.StageConfig),
                asNoTracking: true // Log okurken track etmeye gerek yok, performans artar
            );

            if (run == null) return new List<string>();

            var allLogs = new List<string>();
            var executions = run.StageExecutions.OrderBy(x => x.StageConfig.Order).ToList();

            foreach (var exec in executions)
            {
                if (!string.IsNullOrEmpty(exec.LogsJson))
                {
                    try
                    {
                        var logs = JsonSerializer.Deserialize<List<string>>(exec.LogsJson);
                        if (logs != null) allLogs.AddRange(logs);
                    }
                    catch { }
                }
            }
            return allLogs;
        }

        // --- Helper: Background Job Başlatıcı ---
        private void FireAndForgetRun(int runId)
        {
            _ = Task.Run(async () =>
            {
                using var scope = _sp.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IContentPipelineRunner>();
                try
                {
                    await runner.RunAsync(runId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BACKGROUND ERROR] Run #{runId}: {ex}");
                }
            });
        }
    }
}
