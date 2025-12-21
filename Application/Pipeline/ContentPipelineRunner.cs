using Application.Abstractions;
using Application.Executors;
using Application.Models;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Application.Pipeline
{
    public class ContentPipelineRunner : IContentPipelineRunner
    {
        private readonly IRepository<ContentPipelineRun> _pipelineRepo;
        private readonly IRepository<StageExecution> _stageExecRepo;
        private readonly StageExecutorFactory _executorFactory;
        private readonly IUnitOfWork _uow;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly INotifierService _notifier; // 🔥 SignalR Servisimiz

        private const int MaxRetryCount = 3;

        public ContentPipelineRunner(
            IRepository<ContentPipelineRun> pipelineRepo,
            IRepository<StageExecution> stageExecRepo,
            StageExecutorFactory executorFactory,
            IUnitOfWork uow,
            IServiceScopeFactory scopeFactory,
            INotifierService notifier)
        {
            _pipelineRepo = pipelineRepo;
            _stageExecRepo = stageExecRepo;
            _executorFactory = executorFactory;
            _uow = uow;
            _scopeFactory = scopeFactory;
            _notifier = notifier;
        }

        // -----------------------------------------------------------------------
        // 🔥 HELPER: Merkezi Loglama (Hem DB hem SignalR)
        // -----------------------------------------------------------------------
        private async Task LogAsync(StageExecution exec, string message)
        {
            // 1. Veritabanına kaydet (Kalıcılık)
            exec.AddLog(message);

            // 2. SignalR ile canlı terminale gönder (Anlık İzleme)
            try
            {
                await _notifier.SendLogAsync(exec.ContentPipelineRunId, message);
            }
            catch
            {
                // SignalR hatası ana akışı bozmasın (Fire & Forget)
            }
        }
        // -----------------------------------------------------------------------
        // 🚀 MAIN RUN METHOD
        // -----------------------------------------------------------------------
        public async Task RunAsync(int pipelineRunId, CancellationToken ct)
        {
            // 1. Run verisini ve ilişkili tabloları çek
            var run = await _pipelineRepo.FirstOrDefaultAsync(
                    predicate: r => r.Id == pipelineRunId,
                    include: source => source
                        .Include(r => r.Template)
                            .ThenInclude(t => t.StageConfigs)
                        .Include(r => r.StageExecutions),
                    asNoTracking: false,
                    ct: ct
                );

            if (run == null) throw new InvalidOperationException("Pipeline run bulunamadı.");

            // Zaten bitmişse tekrar çalıştırma
            if (run.Status == ContentPipelineStatus.Completed) return;

            // Durumu güncelle (Eğer WaitingForApproval ise, Service katmanı zaten Processing yapmıştı, dokunmuyoruz)
            if (run.Status != ContentPipelineStatus.WaitingForApproval)
            {
                run.Status = ContentPipelineStatus.Running;
                run.StartedAt ??= DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }

            // Başlangıç Logu
            await _notifier.SendLogAsync(run.Id, "🚀 Pipeline execution started...");

            // Context oluştur ve sıralamayı al
            var context = new PipelineContext(run);
            var stages = run.Template.StageConfigs.OrderBy(s => s.Order).ToList();

            foreach (var stageConfig in stages)
            {
                // Hafızadaki listeden execution kaydını bul
                var exec = run.StageExecutions.FirstOrDefault(x => x.StageConfigId == stageConfig.Id);

                // --- [CRITICAL] REFRESH LOGIC (Ghost Run Önleme) ---
                if (exec != null)
                {
                    var freshExec = await _stageExecRepo.FirstOrDefaultAsync(
                        predicate: x => x.Id == exec.Id,
                        asNoTracking: true,
                        ct: ct
                    );

                    if (freshExec != null)
                    {
                        exec.Status = freshExec.Status;
                        exec.RetryCount = freshExec.RetryCount;
                        exec.OutputJson = freshExec.OutputJson;
                        exec.Error = freshExec.Error;
                    }
                }
                // ----------------------------------------------------

                // Eğer hiç yoksa oluştur (İlk çalışma)
                if (exec == null)
                {
                    exec = new StageExecution
                    {
                        StageConfigId = stageConfig.Id,
                        ContentPipelineRunId = run.Id,
                        Status = StageStatus.Pending
                    };
                    run.StageExecutions.Add(exec);
                    await _uow.SaveChangesAsync(ct);
                }

                // A) EĞER TAMAMLANMIŞSA -> Context'i doldur ve GEÇ (Resume Mantığı)
                if (exec.Status == StageStatus.Completed)
                {
                    HydrateContext(context, stageConfig.StageType, exec.OutputJson);
                    // Zaten biten aşamalar için döngünün sonundaki "Fren Kontrolü"ne girmeden devam et.
                    // Bu sayede Onay sonrası tekrar başladığında Render'ı atlar, direkt Upload'a gider.
                    continue;
                }

                // B) EĞER ÖLMÜŞSE (PermanentlyFailed) -> Tüm Run'ı durdur
                if (exec.Status == StageStatus.PermanentlyFailed)
                {
                    run.Status = ContentPipelineStatus.Failed;
                    run.ErrorMessage = $"Pipeline stopped because stage failed: {stageConfig.StageType}";
                    await _uow.SaveChangesAsync(ct);
                    await _notifier.SendLogAsync(run.Id, $"❌ Pipeline stopped. Stage {stageConfig.StageType} failed.");
                    return;
                }

                // C) 🔥 STAGE ÇALIŞTIR (Esas İşlem) 🔥
                var success = await ExecuteStageAsync(run, stageConfig, exec, context, ct);

                if (!success)
                {
                    run.Status = ContentPipelineStatus.Failed;
                    run.ErrorMessage = exec.Error;
                    await _uow.SaveChangesAsync(ct);
                    await _notifier.SendLogAsync(run.Id, $"❌ Pipeline Failed at {stageConfig.StageType}.");
                    return;
                }

                // =================================================================
                // 🛑 FREN MEKANİZMASI (MANUEL ONAY KONTROLÜ)
                // =================================================================
                // Stage başarıyla bitti. Şimdi "Sıradaki Adım Upload mu?" diye bakıyoruz.

                var nextStage = stages.FirstOrDefault(s => s.Order > stageConfig.Order);

                if (nextStage != null && nextStage.StageType == StageType.Upload)
                {
                    // 🔥 KRİTİK KARAR: AutoPublish AÇIK MI KAPALI MI?

                    if (run.AutoPublish)
                    {
                        // Otomatik Yayın Açık -> Durmak yok, yola devam! 🏎️
                        await _notifier.SendLogAsync(run.Id, "⏩ Auto-Publish enabled. Proceeding to Upload immediately...");
                    }
                    else
                    {
                        // Otomatik Yayın Kapalı -> FREN! 🛑
                        await _notifier.SendLogAsync(run.Id, "✋ Render completed. Stopping for manual approval (AutoPublish: OFF).");

                        run.Status = ContentPipelineStatus.WaitingForApproval;
                        await _uow.SaveChangesAsync(ct);

                        return; // Metottan çık, worker dursun.
                    }
                }
                // =================================================================
            }

            // Hepsi başarıyla bitti
            run.Status = ContentPipelineStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            // Bitiş Logu
            await _notifier.SendLogAsync(run.Id, "✅ Pipeline execution completed successfully! 🎉");
        }

        // -----------------------------------------------------------------------
        // ⚙️ EXECUTE STAGE (Retry & Error Handling)
        // -----------------------------------------------------------------------
        private async Task<bool> ExecuteStageAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            CancellationToken ct)
        {
            var executor = _executorFactory.Resolve(config.StageType);

            await LogAsync(exec, $"▶️ Starting stage: {config.StageType}...");

            for (int attempt = 0; attempt <= MaxRetryCount; attempt++)
            {
                if (attempt > 0)
                {
                    exec.Status = StageStatus.Retrying;
                    exec.RetryCount = attempt;
                    await LogAsync(exec, $"⚠️ Retry attempt {attempt}/{MaxRetryCount} for {config.StageType}...");
                    await _uow.SaveChangesAsync(ct);
                }

                // Executor'ı çalıştır
                // Not: Executor içindeki loglar, eğer executor LogAsync kullanmıyorsa SignalR'a düşmez.
                // İleride Executorlara da loglama yeteneği verebilirsin.
                var result = await executor.ExecuteAsync(run, config, exec, context, ct);

                if (result.Success)
                {
                    // Topic aşaması özel durumu (Run başlığını güncelle)
                    if (config.StageType == StageType.Topic && result.Output is TopicStagePayload topicPayload)
                    {
                        run.RunContextTitle = topicPayload.TopicTitle;
                        run.Language = topicPayload.Language;
                        await _uow.SaveChangesAsync(ct);
                    }

                    await LogAsync(exec, $"✅ Stage {config.StageType} completed.");
                    return true;
                }

                // Hata Durumu
                await LogAsync(exec, $"❌ Attempt {attempt} failed: {result.Error}");

                // Retry Limiti Doldu mu?
                if (attempt == MaxRetryCount)
                {
                    exec.Status = StageStatus.PermanentlyFailed;
                    exec.Error = result.Error ?? "Unknown error";
                    await _uow.SaveChangesAsync(ct);
                    await LogAsync(exec, $"💀 Stage {config.StageType} PERMANENTLY FAILED. Giving up.");
                    return false;
                }

                // Backoff (Bekleme)
                await Task.Delay(1000 * (attempt + 1), ct);
            }

            return false;
        }

        // -----------------------------------------------------------------------
        // 🧠 HYDRATION (Reflection ile Context Doldurma)
        // -----------------------------------------------------------------------
        private void HydrateContext(PipelineContext context, StageType type, string? json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                // Hedef sınıf adını tahmin et: "ScriptStagePayload" vb.
                var className = $"{type}StagePayload";

                // Projedeki tipleri tara ve bul
                var targetType = typeof(ContentPipelineRunner).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name.Equals(className, StringComparison.OrdinalIgnoreCase));

                if (targetType == null)
                {
                    // Kritik hata değil ama loglamak iyidir (SignalR yerine Console'a basıyoruz burayı)
                    Console.WriteLine($"[Warning] Hydration Type Not Found: {className}");
                    return;
                }

                var data = JsonSerializer.Deserialize(json, targetType);

                if (data != null)
                {
                    context.SetOutput(type, data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hydration Error ({type}): {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------
        // 🔁 RETRY LOGIC (Arka Plan Görevi)
        // -----------------------------------------------------------------------
        // Parametreye 'newPresetId' eklendi (Opsiyonel)
        public async Task RetryStageAsync(int runId, string stageTypeStr, int? newPresetId = null, CancellationToken ct = default)
        {
            if (!Enum.TryParse<StageType>(stageTypeStr, true, out var typeEnum))
                throw new ArgumentException($"Geçersiz aşama türü: {stageTypeStr}");

            // 1. Run'ı ve ilgili Stage'i çek
            var run = await _pipelineRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId,
                asNoTracking: false,
                ct: ct,
                include: source => source
                    .Include(r => r.StageExecutions)
                        .ThenInclude(se => se.StageConfig)
            );

            if (run == null) throw new KeyNotFoundException("Run bulunamadı.");

            var stageExec = run.StageExecutions.FirstOrDefault(x => x.StageConfig.StageType == typeEnum);
            if (stageExec == null) throw new KeyNotFoundException($"Run içinde '{stageTypeStr}' aşaması yok.");

            // 🔥 EKLEME 1: Yeni Preset Seçildiyse Güncelle
            if (newPresetId.HasValue)
            {
                // NOT: Eğer StageConfig her Run için kopyalanıyorsa bu güvenlidir.
                // Template'e bağlı ortak config ise dikkat!
                stageExec.StageConfig.PresetId = newPresetId.Value;

                // Log ekleyelim (Senin Notifier yapınla)
                await _notifier.SendLogAsync(run.Id, $"⚙️ Render Preset updated to ID: {newPresetId.Value}");
            }

            // 2. Sicili Temizle (Reset)
            stageExec.Status = StageStatus.Pending;
            stageExec.Error = null;
            stageExec.RetryCount = 0;

            // Eski Output/Log verilerini de temizlemek iyi olabilir
            stageExec.OutputJson = null;

            // 🔥 EKLEME 2: Zincirleme Reaksiyon (Sonraki Aşamaları da Sıfırla)
            // Render tekrar çalışınca oluşan video değişecek. 
            // O yüzden Upload gibi sonraki aşamaların da "Completed" kalması mantıksız olur.
            var currentOrder = stageExec.StageConfig.Order;
            var nextStages = run.StageExecutions
                .Where(x => x.StageConfig.Order > currentOrder)
                .ToList();

            foreach (var next in nextStages)
            {
                next.Status = StageStatus.Pending;
                next.Error = null;
                next.FinishedAt = null;
                // next.OutputJson = null; // Gerekirse
            }

            // Run durumu Failed veya Completed ise tekrar Running'e çek
            if (run.Status == ContentPipelineStatus.Failed || run.Status == ContentPipelineStatus.Completed)
            {
                run.Status = ContentPipelineStatus.Running;
                run.ErrorMessage = null;
            }

            await _uow.SaveChangesAsync(ct);
            await _notifier.SendLogAsync(run.Id, $"🔄 Retry requested for stage: {stageTypeStr}. Pipeline restarting...");

            // 3. Arka Planda Yeniden Başlat (Fire & Forget)
            await Task.Delay(200); // Concurrency önlemi

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var runner = scope.ServiceProvider.GetRequiredService<IContentPipelineRunner>();

                    // Yeni scope ile temiz bir başlangıç
                    await runner.RunAsync(runId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background Retry Failed: {ex.Message}");
                }
            });
        }
    }
}
