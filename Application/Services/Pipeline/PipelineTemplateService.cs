using Application.Contracts.Pipeline;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Pipeline
{
    public class PipelineTemplateService : BaseService<ContentPipelineTemplate>
    {
        private readonly IRepository<StageConfig> _stageRepo;

        public PipelineTemplateService(
            IRepository<ContentPipelineTemplate> repo,
            IRepository<StageConfig> stageRepo,
            IUnitOfWork uow) : base(repo, uow)
        {
            _stageRepo = stageRepo;
        }

        // LIST (Concept Include Edilmeli)
        public async Task<IReadOnlyList<ContentPipelineTemplate>> ListAsync(int userId, string? q, CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: t => t.AppUserId == userId && (string.IsNullOrWhiteSpace(q) || t.Name.Contains(q)),
                orderBy: t => t.CreatedAt,
                desc: true,
                include: src => src.Include(t => t.Concept).Include(t => t.StageConfigs), // StageCount için
                asNoTracking: true,
                ct: ct
            );
        }

        // GET DETAIL (Stages Include Edilmeli)
        public override async Task<ContentPipelineTemplate?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            // Base metodu override ediyoruz çünkü Include lazım
            var entity = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                include: src => src.Include(t => t.StageConfigs),
                asNoTracking: true,
                ct: ct);

            if (entity == null || entity.AppUserId != userId) return null;
            return entity;
        }

        // CREATE
        public async Task<int> CreateAsync(SavePipelineTemplateDto dto, int userId, CancellationToken ct)
        {
            // İsim Çakışması
            if (await _repo.AnyAsync(t => t.AppUserId == userId && t.Name == dto.Name, ct))
                throw new InvalidOperationException("Bu isimde bir şablon zaten var.");

            var entity = new ContentPipelineTemplate
            {
                Name = dto.Name,
                Description = dto.Description,
                ConceptId = dto.ConceptId
                // AppUserId BaseService'de set edilecek
            };

            // Stage'leri ekle
            if (dto.Stages != null)
            {
                int order = 1;
                foreach (var stageDto in dto.Stages)
                {
                    entity.StageConfigs.Add(new StageConfig
                    {
                        StageType = stageDto.StageType,
                        PresetId = stageDto.PresetId,
                        Order = order++ // Otomatik sıra numarası
                    });
                }
            }

            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        // UPDATE
        public async Task UpdateAsync(int id, SavePipelineTemplateDto dto, int userId, CancellationToken ct)
        {
            // Tracking AÇIK çekiyoruz
            var entity = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                include: src => src.Include(t => t.StageConfigs),
                asNoTracking: false,
                ct: ct);

            if (entity == null || entity.AppUserId != userId)
                throw new KeyNotFoundException("Şablon bulunamadı.");

            // Ana bilgileri güncelle
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.ConceptId = dto.ConceptId;

            // STAGE YÖNETİMİ: Eskileri sil, yenileri ekle (En temiz yöntem)
            // Mevcut stage'leri temizle
            entity.StageConfigs.Clear();

            // Yenileri ekle
            if (dto.Stages != null)
            {
                int order = 1;
                foreach (var stageDto in dto.Stages)
                {
                    entity.StageConfigs.Add(new StageConfig
                    {
                        ContentPipelineTemplateId = entity.Id, // ID'yi bağla
                        StageType = stageDto.StageType,
                        PresetId = stageDto.PresetId,
                        Order = order++
                    });
                }
            }

            // Base Update çağırmaya gerek yok, EF Change Tracker halleder ama tarih güncellemek için çağırabiliriz
            entity.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);
        }
    }
}
