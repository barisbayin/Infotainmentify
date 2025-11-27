using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Services
{
    public class StageConfigService
    {
        private readonly IRepository<StageConfig> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public StageConfigService(
            IRepository<StageConfig> repo,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _repo = repo;
            _uow = uow;
            _current = current;
        }

        // ---------------------------------------------------------
        // LIST — pipeline içindeki stage'leri sırayla getir
        // ---------------------------------------------------------
        public async Task<IReadOnlyList<StageConfig>> ListAsync(
            int contentPipelineTemplateId,
            CancellationToken ct = default)
        {
            // User isolation — user kendi pipeline'ını görür
            Expression<Func<StageConfig, bool>> predicate =
                x => x.ContentPipelineTemplateId == contentPipelineTemplateId &&
                     x.ContentPipelineTemplate.AppUserId == _current.UserId;

            var includes = new Expression<Func<StageConfig, object>>[]
            {
            x => x.ContentPipelineTemplate
            };

            var list = await _repo.FindAsync(
                predicate,
                orderBy: x => x.Order,
                desc: false,
                asNoTracking: true,
                ct,
                includes);

            return list;
        }

        // ---------------------------------------------------------
        // GET — tek stage config
        // ---------------------------------------------------------
        public async Task<StageConfig?> GetAsync(int id, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.ContentPipelineTemplate.AppUserId == _current.UserId,
                include: q => q.Include(x => x.ContentPipelineTemplate),
                asNoTracking: true,
                ct: ct);

            return entity;
        }

        // ---------------------------------------------------------
        // CREATE — pipeline içine yeni stage ekle
        // ---------------------------------------------------------
        public async Task<StageConfig> CreateAsync(StageConfig dto, CancellationToken ct)
        {
            // Pipeline kullanıcıya ait mi kontrol et
            if (dto.ContentPipelineTemplate == null && dto.ContentPipelineTemplateId > 0)
            {
                // istersen PipelineService çağırıp check edebilirsin
                // ama minimal kalsın diye burada repo ile check yapıyoruz:
                bool belongsToUser = await _repo.AnyAsync(
                    x => x.ContentPipelineTemplateId == dto.ContentPipelineTemplateId &&
                         x.ContentPipelineTemplate.AppUserId == _current.UserId,
                    ct);

                if (!belongsToUser)
                    throw new InvalidOperationException("Pipeline not found or unauthorized.");
            }

            dto.Id = 0; // güvenlik

            await _repo.AddAsync(dto, ct);
            await _uow.SaveChangesAsync(ct);

            return dto;
        }

        // ---------------------------------------------------------
        // UPDATE — stage içindeki preset/options/order güncelle
        // ---------------------------------------------------------
        public async Task<StageConfig> UpdateAsync(int id, StageConfig dto, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.ContentPipelineTemplate.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                throw new InvalidOperationException("StageConfig not found or unauthorized.");

            entity.StageType = dto.StageType;
            entity.PresetId = dto.PresetId;
            entity.Order = dto.Order;
            entity.OptionsJson = dto.OptionsJson;

            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return entity;
        }

        // ---------------------------------------------------------
        // DELETE — stage pipeline’dan çıkar
        // ---------------------------------------------------------
        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.ContentPipelineTemplate.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                throw new InvalidOperationException("StageConfig not found or unauthorized.");

            _repo.Delete(entity);
            await _uow.SaveChangesAsync(ct);
        }
    }

}
