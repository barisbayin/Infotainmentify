using Application.Executors;
using Application.Models;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using System.Text.Json;

namespace Application.Pipeline
{
    public class ContentPipelineRunner
    {
        private readonly IRepository<ContentPipelineRun> _pipelineRepo;
        // StageExecution repo'su şart değil, run.StageExecutions üzerinden gidebiliriz ama kalsın.
        private readonly IRepository<StageExecution> _stageExecRepo;
        private readonly StageExecutorFactory _executorFactory;
        private readonly IUnitOfWork _uow;

        // Retry Politikası
        private const int MaxRetryCount = 3;

        public ContentPipelineRunner(
            IRepository<ContentPipelineRun> pipelineRepo,
            IRepository<StageExecution> stageExecRepo,
            StageExecutorFactory executorFactory,
            IUnitOfWork uow)
        {
            _pipelineRepo = pipelineRepo;
            _stageExecRepo = stageExecRepo;
            _executorFactory = executorFactory;
            _uow = uow;
        }

        // ----------------------------
        // PIPELINE RUN ENTRY POINT
        // ----------------------------
        public async Task RunAsync(int pipelineRunId, CancellationToken ct)
        {
            // Include'lar önemli! Template ve Config'leri çekmemiz lazım.
            // (Repository yapına göre Include syntax'ın değişebilir)
            var run = await _pipelineRepo.FirstOrDefaultAsync(
                r => r.Id == pipelineRunId,
                false, // Tracking açık olsun
                ct,
                // İlişkileri yükle:
                r => r.Template,
                r => r.StageExecutions
            );

            // Eğer Repository include desteklemiyorsa burada manuel yüklemen gerekebilir.
            // run.Template.StageConfigs'e ihtiyacımız var.

            if (run == null)
                throw new InvalidOperationException("Pipeline run bulunamadı.");

            if (run.Status == ContentPipelineStatus.Completed)
                return;

            run.Status = ContentPipelineStatus.Running;
            run.StartedAt ??= DateTime.UtcNow; // Best practice: UtcNow

            await _uow.SaveChangesAsync(ct);

            // 🔥 1. HAFIZAYI OLUŞTUR (CONTEXT)
            var context = new PipelineContext(run);

            // Sıralamayı garantiye al (Config'leri çekiyoruz)
            // Not: run.Template.StageConfigs null gelirse include eksiktir!
            var stages = run.Template.StageConfigs
                .OrderBy(s => s.Order)
                .ToList();

            foreach (var stageConfig in stages)
            {
                // Mevcut Execution var mı?
                var exec = run.StageExecutions.FirstOrDefault(x => x.StageConfigId == stageConfig.Id);

                if (exec == null)
                {
                    exec = new StageExecution
                    {
                        StageConfigId = stageConfig.Id,
                        ContentPipelineRunId = run.Id,
                        Status = StageStatus.Pending
                    };
                    run.StageExecutions.Add(exec);
                    await _uow.SaveChangesAsync(ct); // Log oluşsun
                }

                // --- RESUME / HYDRATION MANTIĞI ---
                // Eğer stage zaten bitmişse, tekrar çalıştırma AMA verisini hafızaya yükle!
                if (exec.Status == StageStatus.Completed)
                {
                    // ⚠️ KRİTİK: Bir sonraki stage bu veriye ihtiyaç duyabilir.
                    // Veritabanındaki JSON'ı alıp Context'e nesne olarak geri koymalıyız.
                    HydrateContext(context, stageConfig.StageType, exec.OutputJson);
                    continue;
                }

                // Permanent Fail kontrolü
                if (exec.Status == StageStatus.PermanentlyFailed)
                {
                    run.Status = ContentPipelineStatus.Failed;
                    run.ErrorMessage = $"Stage permanently failed: {stageConfig.StageType}";
                    await _uow.SaveChangesAsync(ct);
                    return;
                }

                // 🔥 2. ÇALIŞTIR (Context'i gönderiyoruz)
                var success = await ExecuteStageAsync(run, stageConfig, exec, context, ct);

                if (!success)
                {
                    run.Status = ContentPipelineStatus.Failed;
                    run.ErrorMessage = exec.Error;
                    await _uow.SaveChangesAsync(ct);
                    return;
                }
            }

            // Tüm stage'ler tamam
            run.Status = ContentPipelineStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
        }

        // -----------------------------
        // TEK BİR STAGE ÇALIŞTIRMA
        // -----------------------------
        private async Task<bool> ExecuteStageAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context, // <--- YENİ PARAMETRE
            CancellationToken ct)
        {
            var executor = _executorFactory.Resolve(config.StageType);

            exec.AddLog($"Starting stage {config.StageType}...");

            for (int attempt = 0; attempt <= MaxRetryCount; attempt++)
            {
                if (attempt > 0)
                {
                    exec.Status = StageStatus.Retrying; // Enum ile yönetmek daha güvenli
                    exec.RetryCount = attempt;
                    exec.AddLog($"Retry attempt {attempt}/{MaxRetryCount}...");
                    await _uow.SaveChangesAsync(ct);
                }

                // 🔥 3. EXECUTOR'A CONTEXT VER
                var result = await executor.ExecuteAsync(run, config, exec, context, ct);

                if (result.Success)
                {
                    return true;
                }

                // Fail olduysa logla
                exec.AddLog($"Attempt {attempt} failed: {result.Error}");

                // Retry limiti doldu mu?
                if (attempt == MaxRetryCount)
                {
                    exec.Status = StageStatus.PermanentlyFailed;
                    exec.Error = result.Error ?? "Unknown error";
                    await _uow.SaveChangesAsync(ct);
                    return false;
                }

                // Ufak bir bekleme (backoff) iyi olur
                await Task.Delay(1000 * (attempt + 1), ct);
            }

            return false;
        }

        // -----------------------------
        // 🧠 HAFIZA TAZELEME (HYDRATION)
        // -----------------------------
        private void HydrateContext(PipelineContext context, StageType type, string? json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                // Burası biraz manuel map'leme gerektiriyor.
                // Çünkü JSON string'in hangi class olduğunu StageType'tan anlıyoruz.
                object? data = type switch
                {
                    StageType.Topic => JsonSerializer.Deserialize<TopicStagePayload>(json),
                    StageType.Script => JsonSerializer.Deserialize<string>(json), // Script düz string dönüyorsa
                    // StageType.Image => JsonSerializer.Deserialize<ImageResultPayload>(json),
                    _ => null // Bilinmeyen tipler için
                };

                if (data != null)
                {
                    context.SetOutput(type, data);
                }
            }
            catch (Exception ex)
            {
                // Resume sırasında eski veri bozuksa yapacak çok bir şey yok, logla devam et.
                Console.WriteLine($"Hydration failed for {type}: {ex.Message}");
            }
        }
    }
}
