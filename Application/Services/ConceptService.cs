using Application.Contracts.Concept;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Pipeline;

namespace Application.Services
{
    public class ConceptService : BaseService<Concept>
    {
        public ConceptService(IRepository<Concept> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }

        // LIST (Arama ve Filtreleme)
        public async Task<IReadOnlyList<Concept>> ListAsync(
            int userId, string? q, CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: c =>
                    c.AppUserId == userId &&
                    (string.IsNullOrWhiteSpace(q) || c.Name.Contains(q) || (c.Description != null && c.Description.Contains(q))),
                orderBy: c => c.CreatedAt,
                desc: true,
                asNoTracking: true,
                ct: ct
            );
        }

        // CREATE
        public async Task<int> CreateAsync(SaveConceptDto dto, int userId, CancellationToken ct)
        {
            var name = dto.Name.Trim();

            // Aynı isimde konsept var mı?
            if (await _repo.AnyAsync(c => c.AppUserId == userId && c.Name == name, ct))
                throw new InvalidOperationException("Bu isimde bir konsept zaten var.");

            var entity = new Concept
            {
                Name = name,
                Description = dto.Description
                // AppUserId BaseService tarafından set edilecek
            };

            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        // UPDATE
        public async Task UpdateAsync(int id, SaveConceptDto dto, int userId, CancellationToken ct)
        {
            var entity = await base.GetByIdAsync(id, userId, ct);
            if (entity == null) throw new KeyNotFoundException("Konsept bulunamadı.");

            var name = dto.Name.Trim();

            // İsim değiştiyse unique kontrolü
            if (!string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (await _repo.AnyAsync(c => c.AppUserId == userId && c.Name == name && c.Id != id, ct))
                    throw new InvalidOperationException("Bu isimde başka bir konsept zaten var.");
            }

            entity.Name = name;
            entity.Description = dto.Description;

            await base.UpdateAsync(entity, userId, ct);
        }
    }
}
