using Application.Executors;
using Application.Pipeline;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class PipelineRunnerService
    {
        private readonly IRepository<ContentPipelineRun> _runRepo;
        private readonly StageExecutorFactory _executorFactory;
        private readonly IUnitOfWork _uow;

        public PipelineRunnerService(
            IRepository<ContentPipelineRun> runRepo,
            StageExecutorFactory executorFactory,
            IUnitOfWork uow)
        {
            _runRepo = runRepo;
            _executorFactory = executorFactory;
            _uow = uow;
        }

        public async Task ExecuteRunAsync(int runId, CancellationToken ct = default)
        {
            // 1. Veriyi ve tüm ilişkili tabloları çekiyoruz (Include Zinciri)
            var run = await _runRepo.FirstOrDefaultAsync(
                predicate: r => r.Id == runId,
                include: source => source
                    .Include(r => r.Template)
                        .ThenInclude(t => t.StageConfigs) // Template altındaki configler
                    .Include(r => r.StageExecutions),     // Daha önceki denemeler (Varsa)
                asNoTracking: false, // Değişiklik yapıp kaydedeceğiz, tracking açık olsun
                ct: ct
            );

            if (run == null)
                throw new Exception($"Pipeline Run bulunamadı! ID: {runId}");

            // 2. Run durumunu güncelle: "İşleniyor"
            run.Status = ContentPipelineStatus.Running;
            if (run.StartedAt == null) run.StartedAt = DateTime.Now;
            run.ErrorMessage = null;

            await _uow.SaveChangesAsync(ct);

            // 3. Hafızayı (Context) oluştur
            var context = new PipelineContext(run);

            // 4. Stage'leri çalışma sırasına göre diz
            var orderedConfigs = run.Template.StageConfigs
                .OrderBy(c => c.Order)
                .ToList();

            try
            {
                foreach (var config in orderedConfigs)
                {
                    // Bu konfigürasyon için daha önce bir execution kaydı oluşmuş mu?
                    var execution = run.StageExecutions
                        .FirstOrDefault(x => x.StageConfigId == config.Id);

                    // Yoksa sıfırdan oluştur
                    if (execution == null)
                    {
                        execution = new StageExecution
                        {
                            ContentPipelineRunId = run.Id,
                            StageConfigId = config.Id,
                            Status = StageStatus.Pending
                        };
                        // DB'ye ekle (Logları anlık görebilmek için)
                        run.StageExecutions.Add(execution);
                        await _uow.SaveChangesAsync(ct);
                    }

                    // --- RESUME (KALDIĞI YERDEN DEVAM) MANTIĞI ---
                    if (execution.Status == StageStatus.Completed)
                    {
                        // EĞER BU ADIM ZATEN BİTMİŞSE:
                        // Normalde buradaki OutputJson'ı Context'e geri yüklememiz (Hydrate) gerekir.
                        // Çünkü bir sonraki adım buradaki veriye ihtiyaç duyabilir.
                        // Şimdilik sadece log basıp geçiyoruz. 
                        // *İleri seviye not: Burada outputJson'ı deserialize edip context.SetOutput yapmalıyız.*
                        Console.WriteLine($"Stage {config.StageType} zaten tamamlanmış. Atlanıyor.");
                        continue;
                    }

                    // 5. Executor'ı Fabrikadan Çağır
                    var executor = _executorFactory.Resolve(config.StageType);

                    // 6. ÇALIŞTIR! 🚀
                    // Context burada dolmaya başlıyor
                    var result = await executor.ExecuteAsync(run, config, execution, context, ct);

                    // Her stage sonrası durumu kaydet
                    // (ExecuteAsync içinde zaten MarkCompleted/Failed yapılıyor ama garanti olsun)
                    await _uow.SaveChangesAsync(ct);

                    if (!result.Success)
                    {
                        // Zinciri kır, pipeline dursun.
                        throw new Exception($"Stage Failed ({config.StageType}): {result.Error}");
                    }
                }

                // Döngü sorunsuz biterse
                run.Status = ContentPipelineStatus.Completed;
                run.CompletedAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                run.Status = ContentPipelineStatus.Failed;
                run.ErrorMessage = ex.Message;
                // Loglama servisin varsa buraya eklersin
            }
            finally
            {
                // En son durumu kaydet
                await _uow.SaveChangesAsync(ct);
            }
        }
    }
}
