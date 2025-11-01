using Application.Contracts.Job;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class JobExecutionService
    {
        private readonly IRepository<JobExecution> _execRepo;
        private readonly IRepository<JobSetting> _jobRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public JobExecutionService(
            IRepository<JobExecution> execRepo,
            IRepository<JobSetting> jobRepo,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _execRepo = execRepo;
            _jobRepo = jobRepo;
            _uow = uow;
            _current = current;
        }

        // ---------------- LIST ----------------
        public async Task<IReadOnlyList<JobExecutionListDto>> ListAsync(int? jobId, CancellationToken ct)
        {
            var query = await _execRepo.FindAsync(
                x => x.Job.AppUserId == _current.UserId &&
                     (!jobId.HasValue || x.JobId == jobId),
                include: q => q.Include(x => x.Job),
                asNoTracking: true,
                ct: ct);

            return query
                .OrderByDescending(x => x.StartedAt)
                .Select(x => x.ToListDto())
                .ToList();
        }

        // ---------------- GET DETAIL ----------------
        public async Task<JobExecutionDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            var entity = await _execRepo.FirstOrDefaultAsync(
                x => x.Job.AppUserId == _current.UserId && x.Id == id,
                include: q => q.Include(x => x.Job),
                asNoTracking: true,
                ct: ct);

            return entity?.ToDetailDto();
        }

        // ---------------- START EXECUTION ----------------
        /// <summary>
        /// Yeni bir JobExecution kaydı başlatır.
        /// </summary>
        public async Task<JobExecution> StartExecutionAsync(int jobId, CancellationToken ct)
        {
            var entity = new JobExecution
            {
                JobId = jobId,
                Status = JobStatus.Running,
                StartedAt = DateTimeOffset.Now,
                ResultJson = "{}"
            };

            await _execRepo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            Console.WriteLine($"[JobExecution] Başlatıldı → JobId={jobId}, ExecId={entity.Id}");
            return entity;
        }

        /// <summary>
        /// Job başarıyla tamamlandığında çağrılır.
        /// </summary>
        public async Task CompleteExecutionAsync(int execId, string resultMessage, CancellationToken ct)
        {
            var entity = await _execRepo.GetByIdAsync(execId, false, ct);
            if (entity == null) return;

            entity.Status = JobStatus.Success;
            entity.CompletedAt = DateTimeOffset.Now;
            entity.ResultJson = resultMessage;

            await _uow.SaveChangesAsync(ct);

            Console.WriteLine($"✅ [JobExecution] Tamamlandı → ExecId={execId}, Mesaj={resultMessage}");
        }

        /// <summary>
        /// Job hata ile tamamlandığında çağrılır.
        /// </summary>
        public async Task FailExecutionAsync(int execId, string errorMessage, CancellationToken ct)
        {
            var entity = await _execRepo.GetByIdAsync(execId, false, ct);
            if (entity == null) return;

            entity.Status = JobStatus.Failed;
            entity.CompletedAt = DateTimeOffset.Now;
            entity.ErrorMessage = errorMessage;

            await _uow.SaveChangesAsync(ct);

            Console.WriteLine($"❌ [JobExecution] Hata → ExecId={execId}, Hata={errorMessage}");
        }
    }
}

