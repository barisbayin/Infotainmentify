using Application.Abstractions;
using Application.Contracts.Pipeline;
using Application.Models;
using Application.Pipeline;
using Application.Services;
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
        private readonly IRepository<SavedProductionBrief> _savedBriefRepo;
        private readonly IRepository<ProductionConceptProfile> _conceptProfileRepo;
        private readonly INotifierService _notifier;

        public ContentPipelineService(
            IRepository<ContentPipelineTemplate> templateRepo,
            IRepository<ContentPipelineRun> runRepo,
            IUnitOfWork uow,
            IServiceProvider sp,
            IContentPipelineRunner runner,
            IImageGeneratorService imageGenService,
            IRepository<ImagePreset> imagePresetRepo,
            IRepository<SavedProductionBrief> savedBriefRepo,
            IRepository<ProductionConceptProfile> conceptProfileRepo,
            INotifierService notifier)
        {
            _templateRepo = templateRepo;
            _runRepo = runRepo;
            _uow = uow;
            _sp = sp;
            _runner = runner;
            _imageGenService = imageGenService;
            _imagePresetRepo = imagePresetRepo;
            _savedBriefRepo = savedBriefRepo;
            _conceptProfileRepo = conceptProfileRepo;
            _notifier = notifier;
        }

        public async Task<int> CreateRunAsync(int userId, CreatePipelineRunRequest request, CancellationToken ct)
        {
            var template = await _templateRepo.FirstOrDefaultAsync(
                predicate: t => t.Id == request.TemplateId && t.AppUserId == userId,
                include: source => source.Include(t => t.StageConfigs),
                asNoTracking: true,
                ct: ct);
            if (template == null) throw new KeyNotFoundException("Template bulunamadı.");

            SavedProductionBrief? savedBrief = null;
            if (request.SavedBriefId.HasValue)
            {
                savedBrief = await _savedBriefRepo.FirstOrDefaultAsync(
                    predicate: b => b.Id == request.SavedBriefId.Value && b.AppUserId == userId,
                    asNoTracking: false,
                    ct: ct);

                if (savedBrief == null) throw new KeyNotFoundException("Kayitli brief bulunamadi.");
            }

            var brief = NormalizeBrief(request.Brief);
            if (brief == null && savedBrief != null)
            {
                brief = NormalizeBrief(SavedProductionBriefService.ToProductionBrief(savedBrief));
            }

            if (savedBrief != null)
            {
                savedBrief.LastUsedAt = DateTime.UtcNow;
            }

            var conceptProfile = await _conceptProfileRepo.FirstOrDefaultAsync(
                predicate: p => p.ConceptId == template.ConceptId && p.AppUserId == userId,
                include: source => source.Include(p => p.Concept).Include(p => p.DefaultTemplate!),
                asNoTracking: true,
                ct: ct);

            var run = new ContentPipelineRun
            {
                AppUserId = userId,
                TemplateId = request.TemplateId,
                Status = ContentPipelineStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                AutoPublish = template.AutoPublish,
                InputBriefJson = brief?.ToJson(),
                InputConceptProfileJson = conceptProfile == null ? null : JsonSerializer.Serialize(ConceptProfileService.ToDto(conceptProfile)),
                RunContextTitle = string.IsNullOrWhiteSpace(brief?.MainTitle) ? null : brief.MainTitle.Trim()
            };

            if (request.PauseBeforeRender)
            {
                var renderStage = template.StageConfigs
                    .OrderBy(x => x.Order)
                    .FirstOrDefault(x => x.StageType == StageType.Render);

                if (renderStage != null)
                {
                    run.StageExecutions.Add(new StageExecution
                    {
                        StageConfigId = renderStage.Id,
                        Status = StageStatus.WaitingForApproval,
                        Error = "Render öncesi görsel ve kurgu kontrolü bekleniyor."
                    });
                }
            }

            await _runRepo.AddAsync(run, ct);
            await _uow.SaveChangesAsync(ct);

            if (request.AutoStart)
            {
                FireAndForgetRun(run.Id);
            }

            return run.Id;
        }

        private async Task SendRunLogAsync(int runId, string message)
        {
            try
            {
                await _notifier.SendLogAsync(runId, PipelineLiveLog.WithTimestamp(PipelineLiveLog.Info(message)));
            }
            catch
            {
                // Canlı log hatası ana işlemi bozmasın.
            }
        }

        public async Task StartRunAsync(int userId, int runId, CancellationToken ct)
        {
            var run = await _runRepo.GetByIdAsync(runId, asNoTracking: true, ct);
            if (run == null || run.AppUserId != userId) throw new KeyNotFoundException("Run bulunamadı.");
            if (run.Status == ContentPipelineStatus.Running) throw new InvalidOperationException("Run zaten çalışıyor.");
            if (run.Status is ContentPipelineStatus.Completed or ContentPipelineStatus.Cancelled)
                throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş run tekrar başlatılamaz.");

            FireAndForgetRun(run.Id);
        }

        public async Task CancelRunAsync(int userId, int runId, CancellationToken ct)
        {
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId && r.AppUserId == userId,
                include: src => src
                    .Include(r => r.StageExecutions)
                    .ThenInclude(e => e.StageConfig),
                asNoTracking: false,
                ct: ct);

            if (run == null) throw new KeyNotFoundException("Run bulunamadı.");

            if (run.Status is ContentPipelineStatus.Completed or ContentPipelineStatus.Failed or ContentPipelineStatus.Cancelled)
                throw new InvalidOperationException("Bu run zaten son durumda.");

            PipelineRunCancellationRegistry.Cancel(runId);

            run.Status = ContentPipelineStatus.Cancelled;
            run.CompletedAt ??= DateTime.UtcNow;
            run.ErrorMessage = "Kullanıcı tarafından durduruldu.";

            foreach (var exec in run.StageExecutions.Where(x => x.Status is StageStatus.Running or StageStatus.Retrying))
            {
                exec.MarkCancelled("Kullanıcı tarafından durduruldu.");
            }

            await _uow.SaveChangesAsync(ct);
            await SendRunLogAsync(run.Id, PipelineLiveLog.Warning("Üretim kullanıcı tarafından durduruldu."));
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
            int? videoWidth = null;
            int? videoHeight = null;
            string? videoAspectRatio = null;
            string? thumbnailUrl = null;
            int? thumbnailWidth = null;
            int? thumbnailHeight = null;

            // 1. Render aşamasını bul
            var renderExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == StageType.Render);

            // 2. Eğer bitmişse ve çıktısı varsa JSON'u parse et
            if (renderExec != null && renderExec.Status == StageStatus.Completed && !string.IsNullOrEmpty(renderExec.OutputJson))
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<RenderStagePayload>(renderExec.OutputJson);
                    videoUrl = payload?.VideoUrl; // URL'i kaptık!
                    videoWidth = payload?.Width > 0 ? payload.Width : null;
                    videoHeight = payload?.Height > 0 ? payload.Height : null;
                    videoAspectRatio = payload?.AspectRatio;
                }
                catch { /* JSON bozuksa yapacak bir şey yok, null kalsın */ }
            }

            var thumbnailExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == StageType.Thumbnail);
            if (thumbnailExec != null && thumbnailExec.Status == StageStatus.Completed && !string.IsNullOrEmpty(thumbnailExec.OutputJson))
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<ThumbnailStagePayload>(thumbnailExec.OutputJson);
                    thumbnailUrl = payload?.ThumbnailUrl;
                    thumbnailWidth = payload?.Width > 0 ? payload.Width : null;
                    thumbnailHeight = payload?.Height > 0 ? payload.Height : null;
                }
                catch { /* Thumbnail JSON bozuksa UI boş kalsın. */ }
            }

            return new PipelineRunDetailDto
            {
                Id = run.Id,
                Status = run.Status.ToString(),
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                ErrorMessage = run.ErrorMessage,
                FinalVideoUrl = videoUrl,
                FinalVideoWidth = videoWidth,
                FinalVideoHeight = videoHeight,
                FinalVideoAspectRatio = videoAspectRatio,
                ThumbnailUrl = thumbnailUrl,
                ThumbnailWidth = thumbnailWidth,
                ThumbnailHeight = thumbnailHeight,
                Brief = ProductionBrief.FromJson(run.InputBriefJson),
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
                    OutputJson = null
                }).ToList()
            });
        }

        private static ProductionBrief? NormalizeBrief(ProductionBrief? brief)
        {
            if (brief == null || brief.IsEmpty()) return null;

            return new ProductionBrief
            {
                MainTitle = Clean(brief.MainTitle, 300),
                Angle = Clean(brief.Angle, 1000),
                Audience = Clean(brief.Audience, 500),
                TargetDuration = Clean(brief.TargetDuration, 100),
                MustCover = Clean(brief.MustCover, 2500),
                Avoid = Clean(brief.Avoid, 1500),
                Notes = Clean(brief.Notes, 2500)
            };
        }

        private static string Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var clean = value.Replace("\r", " ").Trim();
            return clean.Length <= maxLength ? clean : clean[..maxLength];
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

        public async Task ApproveRunAsync(int userId, int runId, CancellationToken ct)
        {
            // 1. Run'ı bul
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId && r.AppUserId == userId,
                include: source => source
                    .Include(r => r.StageExecutions)
                    .ThenInclude(e => e.StageConfig),
                asNoTracking: false,
                ct: ct);

            if (run == null)
                throw new KeyNotFoundException($"Pipeline Run with ID {runId} not found.");

            // 2. Business Rule: Gerçekten onay mı bekliyor?
            if (run.Status != ContentPipelineStatus.WaitingForApproval)
            {
                throw new InvalidOperationException("Bu işlem onay beklemiyor. Zaten çalışıyor, bitmiş veya hata almış.");
            }

            var waitingStage = run.StageExecutions
                .OrderBy(x => x.StageConfig.Order)
                .FirstOrDefault(x => x.Status == StageStatus.WaitingForApproval);

            if (waitingStage != null)
            {
                waitingStage.Status = StageStatus.Pending;
                waitingStage.Error = null;
                waitingStage.AddLog("[OK] Manuel onay alındı. Aşama kuyruğa alındı.");
                await SendRunLogAsync(run.Id, $"Manuel onay alındı. Devam edecek aşama: {PipelineLiveLog.StageName(waitingStage.StageConfig.StageType)}.");
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
            var runCts = PipelineRunCancellationRegistry.Register(runId);

            _ = Task.Run(async () =>
            {
                using var scope = _sp.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IContentPipelineRunner>();
                try
                {
                    await runner.RunAsync(runId, runCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Runner kendi status/log kaydını yapar; burada task'ın sessiz bitmesi yeterli.
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BACKGROUND ERROR] Run #{runId}: {ex}");
                }
                finally
                {
                    PipelineRunCancellationRegistry.Complete(runId, runCts);
                }
            });
        }

        public async Task<string> RegenerateSceneImageAsync(int runId, int sceneNumber, int? beatIndex, string? imagePath, CancellationToken ct)
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

            if (sceneNumber <= 0)
                throw new IndexOutOfRangeException("Geçersiz sahne numarası.");

            var targetScene = FindRegenerateTarget(payload.SceneImages, sceneNumber, beatIndex, imagePath);
            if (targetScene == null)
            {
                targetScene = BuildMissingSceneImageTarget(run, run.StageExecutions, preset, sceneNumber, beatIndex, imagePath);
                payload.SceneImages.Add(targetScene);
            }

            var oldImagePath = string.IsNullOrWhiteSpace(targetScene.ImagePath)
                ? imagePath
                : targetScene.ImagePath;
            var targetBeatIndex = targetScene.BeatIndex <= 0 ? beatIndex ?? 1 : targetScene.BeatIndex;

            // 3. AI ile YENİ RESİM ÜRET
            string newImagePath = await _imageGenService.GenerateAndSaveImageAsync(
                userId: run.AppUserId,
                runId: run.Id,
                sceneNumber: targetScene.SceneNumber,
                beatIndex: targetBeatIndex,
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

                    if (layoutPayload?.VisualTrack != null)
                    {
                        var patched = 0;
                        foreach (var visualItem in layoutPayload.VisualTrack.Where(x => IsMatchingVisual(x, targetScene, beatIndex, oldImagePath, imagePath)))
                        {
                            visualItem.ImagePath = newImagePath;
                            visualItem.SourceImageSceneNumber = targetScene.SceneNumber;
                            visualItem.SourceImageBeatIndex = targetBeatIndex;
                            visualItem.IsFallbackImage = false;
                            patched++;
                        }

                        if (patched == 0)
                        {
                            var fallbackVisual = layoutPayload.VisualTrack
                                .FirstOrDefault(x => x.SceneIndex == targetScene.SceneNumber);
                            if (fallbackVisual != null)
                            {
                                fallbackVisual.ImagePath = newImagePath;
                                fallbackVisual.SourceImageSceneNumber = targetScene.SceneNumber;
                                fallbackVisual.SourceImageBeatIndex = targetBeatIndex;
                                fallbackVisual.IsFallbackImage = false;
                            }
                        }

                        if (layoutPayload.EditDecisionList != null)
                        {
                            foreach (var decision in layoutPayload.EditDecisionList.Where(x => IsMatchingDecision(x, targetScene, beatIndex, oldImagePath, imagePath)))
                            {
                                decision.ImagePath = newImagePath;
                                decision.SourceImageSceneNumber = targetScene.SceneNumber;
                                decision.SourceImageBeatIndex = targetBeatIndex;
                                decision.IsFallbackImage = false;
                            }
                        }

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

        private static SceneImageItem BuildMissingSceneImageTarget(
            ContentPipelineRun run,
            ICollection<StageExecution> executions,
            ImagePreset preset,
            int sceneNumber,
            int? beatIndex,
            string? currentImagePath)
        {
            var scriptData = DeserializeStageOutput<ScriptStagePayload>(executions, StageType.Script)
                ?? throw new InvalidOperationException("Eksik görsel üretilemedi: Script çıktısı bulunamadı.");

            var scene = scriptData.Scenes.FirstOrDefault(x => x.SceneNumber == sceneNumber)
                ?? throw new IndexOutOfRangeException($"Sahne {sceneNumber} script içinde bulunamadı.");

            var storyboard = DeserializeStageOutput<StoryboardStagePayload>(executions, StageType.Storyboard);
            var scenePlan = storyboard?.Scenes.FirstOrDefault(x => x.SceneNumber == sceneNumber);
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);
            var visualBeats = ImagePromptComposer.GetVisualBeats(scene, scenePlan, storyboard?.StyleBible, conceptProfile).ToList();
            var requestedBeat = Math.Max(1, beatIndex ?? 1);
            var templateBeat = visualBeats.FirstOrDefault();
            var beat = visualBeats.FirstOrDefault(x => x.BeatIndex == requestedBeat)
                ?? new StoryboardVisualBeat
                {
                    BeatIndex = requestedBeat,
                    BeatRole = requestedBeat == 1
                        ? (string.IsNullOrWhiteSpace(templateBeat?.BeatRole) ? "primary" : templateBeat!.BeatRole)
                        : "manual_repair",
                    VisualPrompt = ProductionPromptContext.FirstNonEmpty(templateBeat?.VisualPrompt, scene.VisualPrompt),
                    ShotType = ProductionPromptContext.FirstNonEmpty(templateBeat?.ShotType, "medium shot"),
                    CameraMotion = ProductionPromptContext.FirstNonEmpty(templateBeat?.CameraMotion, "slow_push_in"),
                    Subject = ProductionPromptContext.FirstNonEmpty(templateBeat?.Subject, "main narration idea"),
                    Composition = ProductionPromptContext.FirstNonEmpty(templateBeat?.Composition, "clean subject-led composition"),
                    Lens = ProductionPromptContext.FirstNonEmpty(templateBeat?.Lens, "35mm documentary lens"),
                    Lighting = ProductionPromptContext.FirstNonEmpty(templateBeat?.Lighting, "motivated soft light"),
                    ColorNotes = templateBeat?.ColorNotes ?? "",
                    ContinuityNotes = templateBeat?.ContinuityNotes ?? "",
                    NegativePrompt = templateBeat?.NegativePrompt ?? "",
                    CutIntent = ProductionPromptContext.FirstNonEmpty(templateBeat?.CutIntent, scene.ScenePurpose, "manual image repair"),
                    OnScreenText = templateBeat?.OnScreenText ?? "",
                    DurationWeight = templateBeat?.DurationWeight > 0 ? templateBeat.DurationWeight : 1.0
                };

            var prompt = ImagePromptComposer.BuildBeatPrompt(preset, scene, scenePlan, storyboard, beat, conceptProfile);
            var beatCount = Math.Max(1, visualBeats.Count);

            return new SceneImageItem
            {
                SceneNumber = sceneNumber,
                BeatIndex = Math.Max(1, beat.BeatIndex),
                BeatCount = beatCount,
                BeatRole = string.IsNullOrWhiteSpace(beat.BeatRole) ? "manual_repair" : beat.BeatRole,
                VisualType = scene.VisualType,
                VarietyRole = scene.VisualVarietyRole,
                VarietyReason = scene.VisualVarietyReason,
                ShotType = beat.ShotType,
                EffectType = NormalizeEffectFromMotion(beat.CameraMotion),
                TransitionType = scenePlan?.TransitionType ?? scene.TransitionIntent ?? "cut",
                OverlayText = !string.IsNullOrWhiteSpace(beat.OnScreenText)
                    ? beat.OnScreenText
                    : scenePlan?.OverlayText ?? scene.OverlayText ?? "",
                DirectorIntent = !string.IsNullOrWhiteSpace(beat.CutIntent)
                    ? beat.CutIntent
                    : scenePlan?.ScenePurpose ?? scene.ScenePurpose ?? "manual image repair",
                ContinuityAnchor = scenePlan?.ContinuityAnchor ?? beat.ContinuityNotes,
                Composition = beat.Composition,
                Lens = beat.Lens,
                Lighting = beat.Lighting,
                ColorNotes = beat.ColorNotes,
                CutIntent = beat.CutIntent,
                ImagePath = currentImagePath ?? "",
                PromptUsed = prompt
            };
        }

        private static T? DeserializeStageOutput<T>(ICollection<StageExecution> executions, StageType stageType)
        {
            var exec = executions
                .OrderByDescending(x => x.Id)
                .FirstOrDefault(x => x.StageConfig?.StageType == stageType);

            if (string.IsNullOrWhiteSpace(exec?.OutputJson))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(exec.OutputJson);
            }
            catch
            {
                return default;
            }
        }

        private static string NormalizeEffectFromMotion(string? cameraMotion)
        {
            var token = (cameraMotion ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            return token switch
            {
                "slow_pull_out" or "pull_out" or "zoom_out" => "zoom_out",
                "pan_left" => "pan_left",
                "pan_right" => "pan_right",
                "pan_up" => "pan_up",
                "pan_down" => "pan_down",
                "static" or "static_hold" => "static",
                _ => "zoom_in"
            };
        }

        private static SceneImageItem? FindRegenerateTarget(
            List<SceneImageItem> sceneImages,
            int sceneNumber,
            int? beatIndex,
            string? imagePath)
        {
            var sceneMatches = sceneImages
                .Where(x => x.SceneNumber == sceneNumber)
                .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                .ToList();

            if (sceneMatches.Count == 0) return null;

            var requestedFileName = GetPortableFileNameOrEmpty(imagePath);
            if (!string.IsNullOrWhiteSpace(requestedFileName))
            {
                var byPath = sceneMatches.FirstOrDefault(x =>
                    string.Equals(GetPortableFileNameOrEmpty(x.ImagePath), requestedFileName, StringComparison.OrdinalIgnoreCase));

                if (byPath != null) return byPath;
            }

            if (beatIndex.HasValue)
            {
                var byBeat = sceneMatches.FirstOrDefault(x => x.BeatIndex == beatIndex.Value);
                if (byBeat != null) return byBeat;
            }

            if (beatIndex.HasValue || !string.IsNullOrWhiteSpace(requestedFileName))
                return null;

            return sceneMatches.FirstOrDefault();
        }

        private static bool IsMatchingVisual(
            VisualEvent visual,
            SceneImageItem target,
            int? beatIndex,
            string? oldImagePath,
            string? requestedImagePath)
        {
            if (visual.SceneIndex != target.SceneNumber) return false;

            var visualFile = GetPortableFileNameOrEmpty(visual.ImagePath);
            var oldFile = GetPortableFileNameOrEmpty(oldImagePath);
            var requestedFile = GetPortableFileNameOrEmpty(requestedImagePath);

            if (!string.IsNullOrWhiteSpace(requestedFile)
                && string.Equals(visualFile, requestedFile, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(oldFile)
                && string.Equals(visualFile, oldFile, StringComparison.OrdinalIgnoreCase))
                return true;

            return beatIndex.HasValue
                   && (visual.SegmentIndex == beatIndex.Value
                       || visual.SourceImageBeatIndex == beatIndex.Value);
        }

        private static bool IsMatchingDecision(
            EditDecisionItem decision,
            SceneImageItem target,
            int? beatIndex,
            string? oldImagePath,
            string? requestedImagePath)
        {
            if (decision.SceneNumber != target.SceneNumber) return false;

            var decisionFile = GetPortableFileNameOrEmpty(decision.ImagePath);
            var oldFile = GetPortableFileNameOrEmpty(oldImagePath);
            var requestedFile = GetPortableFileNameOrEmpty(requestedImagePath);

            if (!string.IsNullOrWhiteSpace(requestedFile)
                && string.Equals(decisionFile, requestedFile, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(oldFile)
                && string.Equals(decisionFile, oldFile, StringComparison.OrdinalIgnoreCase))
                return true;

            return beatIndex.HasValue
                   && (decision.SourceImageBeatIndex == beatIndex.Value
                       || string.IsNullOrWhiteSpace(decision.ImagePath));
        }

        private static string GetPortableFileNameOrEmpty(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";

            try
            {
                var normalized = Uri.UnescapeDataString(path)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);

                return Path.GetFileName(normalized);
            }
            catch
            {
                return "";
            }
        }
    }
}
