using Application.Abstractions;
using Application.Contracts.Pipeline;
using Application.Models;
using Application.Services.Interfaces;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
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
        private readonly IImageGeneratorService _imageGenService;
        private readonly IRepository<ImagePreset> _imagePresetRepo;

        public ContentPipelineService(
            IRepository<ContentPipelineTemplate> templateRepo,
            IRepository<ContentPipelineRun> runRepo,
            IUnitOfWork uow,
            IServiceProvider sp,
            IContentPipelineRunner runner,
            IImageGeneratorService imageGenService,
            IRepository<ImagePreset> imagePresetRepo)
        {
            _templateRepo = templateRepo;
            _runRepo = runRepo;
            _uow = uow;
            _sp = sp;
            _runner = runner;
            _imageGenService = imageGenService;
            _imagePresetRepo = imagePresetRepo;
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
                CreatedAt = DateTime.UtcNow,
                AutoPublish = template.AutoPublish
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
                // 🔥 1. DEĞİŞİKLİK: Include zincirini genişletiyoruz
                include: source => source
                    .Include(r => r.Template)
                        .ThenInclude(t => t.Concept) // Konsept Adı için
                    .Include(r => r.StageExecutions)
                        .ThenInclude(e => e.StageConfig), // İkonlar (Upload mu?) anlamak için
                asNoTracking: true,
                ct: ct
            );

            // 🔥 2. DEĞİŞİKLİK: Mapleme (Mapping) işlemini güncelliyoruz
            return runs.Select(r => new PipelineRunListDto
            {
                // -- Mevcut Alanlar --
                Id = r.Id,
                TemplateName = r.Template?.Name ?? "Silinmiş Şablon",
                Status = r.Status.ToString(),
                StartedAt = r.StartedAt,
                CompletedAt = r.CompletedAt,

                // Başlık mantığı: Önce DB'deki başlığa bak, yoksa Script çıktısından bulmaya çalış, o da yoksa Şablon adını bas.
                RunContextTitle = !string.IsNullOrEmpty(r.RunContextTitle)
                    ? r.RunContextTitle
                    : ExtractTitleFromScript(r.StageExecutions),

                // -- Yeni Eklenen Alanlar --
                ConceptName = r.Template?.Concept?.Name ?? "-",

                // Frontend'deki ikonlar için gerekli liste
                StageExecutions = r.StageExecutions.Select(e => new StageExecutionSummaryDto
                {
                    Id = e.Id,
                    // StageConfig silinmişse patlamasın diye kontrol
                    StageType = e.StageConfig?.StageType.ToString() ?? "Unknown",
                    Status = e.Status.ToString(),
                    OutputJson = e.OutputJson // Linkler bunun içinde
                }).ToList()
            });
        }

        // --- YARDIMCI METOD (Sınıfın altına ekle) ---
        private string? ExtractTitleFromScript(ICollection<StageExecution> executions)
        {
            // Script aşamasını bul
            var scriptExec = executions.FirstOrDefault(x => x.StageConfig?.StageType == StageType.Script);

            // Çıktısı varsa ve başarılıysa parse et
            if (scriptExec != null && !string.IsNullOrEmpty(scriptExec.OutputJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(scriptExec.OutputJson);
                    if (doc.RootElement.TryGetProperty("Title", out var titleProp))
                    {
                        return titleProp.GetString();
                    }
                }
                catch { /* JSON bozuksa sessizce geç */ }
            }
            return null;
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

        public async Task ApproveRunAsync(int runId, CancellationToken ct)
        {
            // 1. Run'ı bul
            var run = await _runRepo.GetByIdAsync(runId);

            if (run == null)
                throw new KeyNotFoundException($"Pipeline Run with ID {runId} not found.");

            // 2. Business Rule: Gerçekten onay mı bekliyor?
            if (run.Status != ContentPipelineStatus.WaitingForApproval)
            {
                throw new InvalidOperationException("Bu işlem onay beklemiyor. Zaten çalışıyor, bitmiş veya hata almış.");
            }

            // 3. Statüyü güncelle (Kaldığı yerden devam etmesi için 'Processing' yapıyoruz)
            run.Status = ContentPipelineStatus.Running;

            // 4. Kaydet
            await _uow.SaveChangesAsync(ct);

            // 5. 🔥 MOTORU TETİKLE (Fire and Forget)
            // Controller beklemesin diye Task.Run ile arka plana atıyoruz.
            // Not: Kendi sınıfımız içindeki RunAsync'i (veya ExecuteRunAsync) çağırıyoruz.
            FireAndForgetRun(run.Id);
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

        public async Task<string> RegenerateSceneImageAsync(int runId, int sceneIndex, CancellationToken ct)
        {
            // 1. Run'ı ve ImageStage outputunu bul
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId,
                include: src => src
                    .Include(x => x.StageExecutions)
                    .ThenInclude(x => x.StageConfig),
                asNoTracking: false, // Update yapacağız, tracking açık kalsın
                ct: ct
            );

            if (run == null) throw new KeyNotFoundException("Run bulunamadı.");

            // Image Stage'ini bul
            var imageExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == StageType.Image);
            if (imageExec == null) throw new InvalidOperationException("Image stage bulunamadı.");

            // Preset Verisini ID ile Çekme
            int presetId = imageExec.StageConfig.PresetId ?? 0;
            if (presetId == 0) throw new InvalidOperationException("Bu aşama için bir Preset ID tanımlanmamış.");

            var preset = await _imagePresetRepo.GetByIdAsync(presetId);
            if (preset == null) throw new KeyNotFoundException($"Preset (ID: {presetId}) veritabanında bulunamadı.");

            // 2. Mevcut Image JSON'ı Deserialize et
            if (string.IsNullOrEmpty(imageExec.OutputJson)) throw new InvalidOperationException("Henüz görsel üretilmemiş.");

            var payload = JsonSerializer.Deserialize<ImageStagePayload>(imageExec.OutputJson);
            if (payload == null || payload.SceneImages == null) throw new InvalidOperationException("Görsel verisi bozuk.");

            if (sceneIndex < 0 || sceneIndex >= payload.SceneImages.Count)
                throw new IndexOutOfRangeException("Geçersiz sahne indeksi.");

            var targetScene = payload.SceneImages[sceneIndex];

            // 3. AI ile YENİ RESİM ÜRET
            string newImagePath = await _imageGenService.GenerateAndSaveImageAsync(
                userId: run.AppUserId,
                runId: run.Id,
                sceneNumber: targetScene.SceneNumber,
                prompt: targetScene.PromptUsed,
                connectionId: preset.UserAiConnectionId,
                preset: preset,
                ct: ct
            );

            // 4. Image Listesini Güncelle
            targetScene.ImagePath = newImagePath;

            // Image JSON'ı tekrar paketle ve DB'ye yazılmaya hazır hale getir
            imageExec.OutputJson = JsonSerializer.Serialize(payload);

            // =================================================================================
            // 🔥 KRİTİK EKLEME: SCENE LAYOUT (TIMELINE) PATCH İŞLEMİ
            // Bunu yapmazsan Render alırken eski resmi kullanmaya devam eder!
            // =================================================================================
            var layoutExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == StageType.SceneLayout);

            if (layoutExec != null && !string.IsNullOrEmpty(layoutExec.OutputJson))
            {
                try
                {
                    var layoutPayload = JsonSerializer.Deserialize<SceneLayoutStagePayload>(layoutExec.OutputJson);

                    // Layout içindeki ilgili sahneyi bul (SceneIndex eşleşmesi)
                    // Not: targetScene.SceneNumber senin sisteminde 1'den başlıyorsa burası doğrudur.
                    var visualItem = layoutPayload?.VisualTrack?.FirstOrDefault(x => x.SceneIndex == targetScene.SceneNumber);

                    if (visualItem != null)
                    {
                        // Sadece yolu değiştiriyoruz, süreler ve efektler korunuyor
                        visualItem.ImagePath = newImagePath;

                        // Güncellenmiş Layout JSON'ını geri yaz
                        layoutExec.OutputJson = JsonSerializer.Serialize(layoutPayload);
                    }
                }
                catch (Exception ex)
                {
                    // Layout güncelleme hatası akışı bozmasın, loglayıp geçebilirsin
                    Console.WriteLine($"[Warning] Layout patch failed: {ex.Message}");
                }
            }
            // =================================================================================

            // 6. RENDER'ı Güncelliğini Yitirdi (Outdated) Olarak İşaretle
            var renderExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == StageType.Render);

            if (renderExec != null)
            {
                // Eğer render daha önce tamamlanmışsa veya hata almışsa,
                // yeni resim geldiği için artık "Eski" (Outdated) durumuna düşer.
                if (renderExec.Status == StageStatus.Completed || renderExec.Status == StageStatus.Failed)
                {
                    renderExec.Status = StageStatus.Outdated;
                }
                // Eğer zaten Pending veya Processing ise dokunmaya gerek yok.
            }

            // Değişiklikleri Kaydet (Hem ImageStage, Hem SceneLayout, Hem RenderStatus)
            await _uow.SaveChangesAsync(ct);

            return newImagePath;
        }
    }
}
