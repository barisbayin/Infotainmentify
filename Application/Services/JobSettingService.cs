using Application.Contracts.Job;
using Application.Job;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Reflection;
using TriggerBuilder = Quartz.TriggerBuilder;

namespace Application.Services
{
    public class JobSettingService
    {
        private readonly IRepository<JobSetting> _jobRepo;
        private readonly IRepository<JobExecution> _execRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly ISchedulerFactory _schedulerFactory;

        public JobSettingService(
            IRepository<JobSetting> jobRepo,
            IRepository<JobExecution> execRepo,
            IUnitOfWork uow,
            ICurrentUserService current,
            ISchedulerFactory schedulerFactory)
        {
            _jobRepo = jobRepo;
            _execRepo = execRepo;
            _uow = uow;
            _current = current;
            _schedulerFactory = schedulerFactory;
        }

        public async Task<IReadOnlyList<JobSettingListDto>> ListAsync(CancellationToken ct)
        {
            var jobs = await _jobRepo.FindAsync(
                x => x.AppUserId == _current.UserId,
                asNoTracking: true,
                ct: ct);

            return jobs.Select(x => x.ToListDto()).ToList();
        }

        public async Task<JobSettingDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            var entity = await _jobRepo.FirstOrDefaultAsync(
                x => x.AppUserId == _current.UserId && x.Id == id,
                asNoTracking: false,
                ct: ct);

            return entity?.ToDetailDto();
        }

        public async Task<JobSettingDetailDto?> GetNoUserAsync(int id, CancellationToken ct)
        {
            var entity = await _jobRepo.FirstOrDefaultAsync(
                x => x.Id == id,
                asNoTracking: false,
                ct: ct);

            return entity?.ToDetailDto();
        }

        public Task<JobSetting?> GetEntityAsync(int id, CancellationToken ct)
        => _jobRepo.FirstOrDefaultAsync(
        x => x.AppUserId == _current.UserId && x.Id == id,
        asNoTracking: false,
        ct: ct);

        public Task<JobSetting?> GetByIdNoUserAsync(int id, CancellationToken ct)
        => _jobRepo.FirstOrDefaultAsync(x => x.Id == id, asNoTracking: false, ct: ct);

        public async Task<int> UpsertAsync(JobSettingDetailDto dto, CancellationToken ct)
        {
            var userId = _current.UserId;
            JobSetting entity;

            var profileTypeName = JobProfileMapper.GetProfileAssemblyQualifiedName(
                Enum.Parse<JobType>(dto.JobType, true)
            ) ?? throw new InvalidOperationException($"Profile eşlemesi bulunamadı: {dto.JobType}");

            if (dto.Id == 0)
            {
                entity = new JobSetting
                {
                    AppUserId = userId,
                    JobType = Enum.Parse<JobType>(dto.JobType, true),
                    Name = dto.Name.Trim(),
                    ProfileId = dto.ProfileId,
                    ProfileType = profileTypeName,
                    IsAutoRunEnabled = dto.IsAutoRunEnabled,
                    PeriodHours = dto.PeriodHours,
                    Status = JobStatus.Pending
                };
                await _jobRepo.AddAsync(entity, ct);
            }
            else
            {
                entity = await _jobRepo.FirstOrDefaultAsync(
                    x => x.AppUserId == userId && x.Id == dto.Id,
                    asNoTracking: false, ct: ct
                ) ?? throw new InvalidOperationException("Job bulunamadı.");

                entity.JobType = Enum.Parse<JobType>(dto.JobType, true);
                entity.Name = dto.Name.Trim();
                entity.ProfileId = dto.ProfileId;
                entity.ProfileType = profileTypeName;
                entity.IsAutoRunEnabled = dto.IsAutoRunEnabled;
                entity.PeriodHours = dto.PeriodHours;
                entity.Status = Enum.TryParse<JobStatus>(dto.Status, true, out var parsed)
                    ? parsed
                    : JobStatus.Pending; // ✅ dönüşüm eklendi
                entity.LastError = dto.LastError;
                entity.LastRunAt = dto.LastRunAt;

                _jobRepo.Update(entity);
            }

            await _uow.SaveChangesAsync(ct);
            await RescheduleJobAsync(entity, ct);

            return entity.Id;
        }



        public async Task RescheduleJobAsync(JobSetting job, CancellationToken ct)
        {
            var scheduler = await _schedulerFactory.GetScheduler(ct);
            var jobKey = new JobKey($"job_{job.Id}", "default");

            if (await scheduler.CheckExists(jobKey, ct))
                await scheduler.DeleteJob(jobKey, ct);

            if (!job.IsAutoRunEnabled)
            {
                Console.WriteLine($"[Quartz] Job devre dışı bırakıldı → {job.Name}");
                return;
            }

            var jobDetail = JobBuilder.Create<BaseQuartzJob>()
                .WithIdentity(jobKey)
                .UsingJobData("JobId", job.Id)
                .Build();

            var hours = job.PeriodHours ?? 24m;
            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger_{job.Id}", "default")
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromHours((double)hours)).RepeatForever())
                .Build();

            await scheduler.ScheduleJob(jobDetail, trigger, ct);
            Console.WriteLine($"[Quartz] Job planlandı → {job.Name} ({hours} saatte bir)");
        }

        public async Task<List<JobSetting>> GetAutoRunJobsAsync(CancellationToken ct)
        {
            var list = await _jobRepo.FindAsync(
                x => x.IsAutoRunEnabled && !x.Removed && x.IsActive && x.User.IsActive, 
                include: q => q.Include(x => x.User),
                asNoTracking: true,
                ct: ct
            );

            return list.ToList();
        }



        public async Task<IJobProfile?> ResolveProfileAsync(JobSetting job, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(job.ProfileType))
                throw new InvalidOperationException("ProfileType boş olamaz.");

            string typeName = job.ProfileType.Split(',')[0].Trim(); // "Core.Entity.TopicGenerationProfile"
            Type? type = Type.GetType(job.ProfileType, throwOnError: false);

            if (type == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

                // 🔥 Core assembly yüklü değilse manuel yükle
                if (!assemblies.Any(a => a.GetName().Name == "Core"))
                {
                    var corePath = Path.Combine(AppContext.BaseDirectory, "Core.dll");
                    if (File.Exists(corePath))
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(corePath);
                            assemblies.Add(asm);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Core.dll yüklenemedi: {ex.Message}");
                        }
                    }
                }

                // 🔍 Tüm assembly'lerde tara
                type = assemblies
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
            }

            if (type == null)
                throw new InvalidOperationException($"Profile tipi çözülemedi: {job.ProfileType}");

            // 🔄 EF üzerinden yükle
            var db = _uow.GetDbContext();
            var entity = await db.FindAsync(type, job.ProfileId, ct);

            if (entity is not IJobProfile profile)
                throw new InvalidOperationException($"Profile '{type.Name}' IJobProfile arayüzünü implemente etmiyor.");

            return profile;
        }






        /// <summary>
        /// Job siler ve Quartz’tan da kaldırır.
        /// </summary>
        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var entity = await _jobRepo.FirstOrDefaultAsync(
                x => x.AppUserId == _current.UserId && x.Id == id,
                asNoTracking: false,
                ct: ct
            );

            if (entity == null)
                return false;

            _jobRepo.Delete(entity);
            await _uow.SaveChangesAsync(ct);

            // Quartz job'unu da sil
            var scheduler = await _schedulerFactory.GetScheduler(ct);
            var jobKey = new JobKey($"job_{entity.Id}", "default");

            if (await scheduler.CheckExists(jobKey, ct))
                await scheduler.DeleteJob(jobKey, ct);

            Console.WriteLine($"[Quartz] Job silindi → {entity.Name}");
            return true;
        }

        /// <summary>
        /// Job'u manuel olarak çalıştırır (kullanıcı tetiklediğinde).
        /// </summary>
        public async Task TriggerJobAsync(int id, CancellationToken ct)
        {
            var scheduler = await _schedulerFactory.GetScheduler(ct);
            var jobKey = new JobKey($"job_{id}", "default");

            if (!await scheduler.CheckExists(jobKey, ct))
            {
                var job = await _jobRepo.GetByIdAsync(id, asNoTracking: true, ct);
                if (job is null)
                    throw new InvalidOperationException("Job bulunamadı.");

                var jobDetail = JobBuilder.Create<BaseQuartzJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("JobId", job.Id)
                    .Build();

                await scheduler.AddJob(jobDetail, true, true, ct);
            }

            await scheduler.TriggerJob(jobKey, ct);
        }

        public async Task ToggleAutoRunAsync(int id, bool enable, CancellationToken ct)
        {
            var entity = await _jobRepo.FirstOrDefaultAsync(
                x => x.AppUserId == _current.UserId && x.Id == id,
                asNoTracking: false,
                ct: ct
            ) ?? throw new InvalidOperationException("Job bulunamadı.");

            entity.IsAutoRunEnabled = enable;
            _jobRepo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            await RescheduleJobAsync(entity, ct);
        }

    }
}
