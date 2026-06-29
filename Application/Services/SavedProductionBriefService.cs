using Application.Contracts.ProductionBriefs;
using Application.Models;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class SavedProductionBriefService : BaseService<SavedProductionBrief>
    {
        private readonly IRepository<Concept> _conceptRepo;

        public SavedProductionBriefService(
            IRepository<SavedProductionBrief> repo,
            IRepository<Concept> conceptRepo,
            IUnitOfWork uow) : base(repo, uow)
        {
            _conceptRepo = conceptRepo;
        }

        public async Task<IReadOnlyList<SavedProductionBrief>> ListAsync(
            int userId,
            string? q,
            int? conceptId,
            CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: b =>
                    b.AppUserId == userId &&
                    (!conceptId.HasValue || b.ConceptId == conceptId) &&
                    (string.IsNullOrWhiteSpace(q)
                        || b.Name.Contains(q)
                        || (b.MainTitle != null && b.MainTitle.Contains(q))
                        || (b.Angle != null && b.Angle.Contains(q))),
                orderBy: b => b.UpdatedAt ?? b.CreatedAt,
                desc: true,
                include: source => source.Include(b => b.Concept),
                asNoTracking: true,
                ct: ct);
        }

        public async Task<SavedProductionBrief?> GetDetailAsync(int id, int userId, CancellationToken ct)
        {
            return await _repo.FirstOrDefaultAsync(
                predicate: b => b.Id == id && b.AppUserId == userId,
                include: source => source.Include(b => b.Concept),
                asNoTracking: true,
                ct: ct);
        }

        public async Task<int> CreateAsync(SaveProductionBriefDto dto, int userId, CancellationToken ct)
        {
            await ValidateConceptAsync(dto.ConceptId, userId, ct);

            var entity = new SavedProductionBrief();
            Apply(entity, dto);

            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        public async Task UpdateAsync(int id, SaveProductionBriefDto dto, int userId, CancellationToken ct)
        {
            await ValidateConceptAsync(dto.ConceptId, userId, ct);

            var entity = await _repo.FirstOrDefaultAsync(
                predicate: b => b.Id == id && b.AppUserId == userId,
                asNoTracking: false,
                ct: ct);

            if (entity == null) throw new KeyNotFoundException("Brief bulunamadi.");

            Apply(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
        }

        public static SavedProductionBriefDto ToDto(SavedProductionBrief entity)
        {
            return new SavedProductionBriefDto
            {
                Id = entity.Id,
                ConceptId = entity.ConceptId,
                ConceptName = entity.Concept?.Name,
                Name = entity.Name,
                MainTitle = entity.MainTitle,
                Angle = entity.Angle,
                Audience = entity.Audience,
                TargetDuration = entity.TargetDuration,
                MustCover = entity.MustCover,
                Avoid = entity.Avoid,
                Notes = entity.Notes,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                LastUsedAt = entity.LastUsedAt
            };
        }

        public static ProductionBrief ToProductionBrief(SavedProductionBrief entity)
        {
            return new ProductionBrief
            {
                MainTitle = entity.MainTitle ?? "",
                Angle = entity.Angle ?? "",
                Audience = entity.Audience ?? "",
                TargetDuration = entity.TargetDuration ?? "",
                MustCover = entity.MustCover ?? "",
                Avoid = entity.Avoid ?? "",
                Notes = entity.Notes ?? ""
            };
        }

        private async Task ValidateConceptAsync(int? conceptId, int userId, CancellationToken ct)
        {
            if (!conceptId.HasValue) return;

            var exists = await _conceptRepo.AnyAsync(
                c => c.Id == conceptId.Value && c.AppUserId == userId,
                ct);

            if (!exists) throw new KeyNotFoundException("Konsept bulunamadi.");
        }

        private static void Apply(SavedProductionBrief entity, SaveProductionBriefDto dto)
        {
            entity.ConceptId = dto.ConceptId;
            entity.Name = Clean(dto.Name, 160);
            entity.MainTitle = CleanNullable(dto.MainTitle, 300);
            entity.Angle = CleanNullable(dto.Angle, 1000);
            entity.Audience = CleanNullable(dto.Audience, 500);
            entity.TargetDuration = CleanNullable(dto.TargetDuration, 100);
            entity.MustCover = CleanNullable(dto.MustCover, 2500);
            entity.Avoid = CleanNullable(dto.Avoid, 1500);
            entity.Notes = CleanNullable(dto.Notes, 2500);
        }

        private static string Clean(string value, int maxLength)
        {
            var clean = (value ?? "").Replace("\r", " ").Trim();
            if (string.IsNullOrWhiteSpace(clean)) throw new InvalidOperationException("Brief adi zorunludur.");
            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }

        private static string? CleanNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var clean = value.Replace("\r", " ").Trim();
            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }
    }
}
