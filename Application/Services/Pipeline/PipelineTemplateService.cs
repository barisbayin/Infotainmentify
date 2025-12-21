using Application.Contracts.Pipeline;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
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

        public async Task<IReadOnlyList<ContentPipelineTemplate>> ListAsync(
                    int userId,
                    string? q,
                    int? conceptId, // 🔥 YENİ PARAMETRE
                    CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: t =>
                    t.AppUserId == userId &&
                    (!conceptId.HasValue || t.ConceptId == conceptId) && // 🔥 FİLTRE
                    (string.IsNullOrWhiteSpace(q) || t.Name.Contains(q)),
                orderBy: t => t.CreatedAt,
                desc: true,
                include: src => src.Include(t => t.Concept).Include(t => t.StageConfigs),
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
                ConceptId = dto.ConceptId,
                AutoPublish = dto.AutoPublish,
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
                        Order = order++, // Otomatik sıra numarası
                        OptionsJson = stageDto.OptionsJson
                    });
                }
            }

            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        // UPDATE
        public async Task UpdateAsync(int id, SavePipelineTemplateDto dto, int userId, CancellationToken ct)
        {
            // 1. Template'i ve Mevcut Stage'lerini ÇEK (Tracking AÇIK)
            // Include yapıyoruz çünkü var olan satırları güncelleyeceğiz.
            var entity = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                include: src => src.Include(t => t.StageConfigs),
                asNoTracking: false,
                ct: ct);

            if (entity == null || entity.AppUserId != userId)
                throw new KeyNotFoundException("Şablon bulunamadı.");

            // 2. Ana bilgileri güncelle
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.ConceptId = dto.ConceptId;
            entity.AutoPublish = dto.AutoPublish;

            // =================================================================
            // 🔥 AKILLI EŞİTLEME (SMART SYNC)
            // Silip yeniden eklemek yerine, eldekileri güncelliyoruz.
            // =================================================================

            var newStages = dto.Stages ?? new List<SaveStageConfigDto>();
            var existingStages = entity.StageConfigs.OrderBy(x => x.Order).ToList();

            // Döngü ile eşleştirme yapıyoruz
            int maxCount = Math.Max(newStages.Count, existingStages.Count);

            for (int i = 0; i < maxCount; i++)
            {
                if (i < newStages.Count && i < existingStages.Count)
                {
                    // A) İKİSİ DE VAR -> GÜNCELLE (Recycle)
                    // Mevcut satırı al, yeni verilerle güncelle. ID ve Log bağlantısı korunur.
                    var existing = existingStages[i];
                    var newVal = newStages[i];

                    existing.StageType = newVal.StageType;
                    existing.PresetId = newVal.PresetId == 0 ? null : newVal.PresetId;
                    existing.Order = i + 1;
                    existing.OptionsJson = newVal.OptionsJson;

                    // Soft delete olmuşsa geri getir (Eğer sistemde varsa)
                    // existing.IsDeleted = false; 
                }
                else if (i < newStages.Count)
                {
                    // B) YENİSİ FAZLA -> EKLE (Insert)
                    var newVal = newStages[i];
                    entity.StageConfigs.Add(new StageConfig
                    {
                        StageType = newVal.StageType,
                        PresetId = newVal.PresetId == 0 ? null : newVal.PresetId,
                        Order = i + 1,
                        OptionsJson = newVal.OptionsJson
                        // TemplateId otomatik set edilir
                    });
                }
                else
                {
                    // C) ESKİSİ FAZLA -> SİL (Soft Delete)
                    // Fazlalık olan satırı Repo üzerinden siliyoruz.
                    // Repo'da Soft Delete varsa "Removed=1" yapar, fiziksel silmez. Hata vermez.
                    var extra = existingStages[i];
                    _stageRepo.Delete(extra);
                }
            }

            entity.UpdatedAt = DateTime.UtcNow;

            // 3. Kaydet
            await _uow.SaveChangesAsync(ct);
        }
    }
}
