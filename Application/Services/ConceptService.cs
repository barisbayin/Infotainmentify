using Application.Contracts.Concept;
using Core.Contracts;
using Core.Entity.Pipeline;

namespace Application.Services
{
    public class ConceptService
    {
        private readonly IRepository<Concept> _repo;
        private readonly IUnitOfWork _uow;

        public ConceptService(IRepository<Concept> repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        // ------------------------------
        // LIST
        // ------------------------------
        public Task<IReadOnlyList<Concept>> ListAsync(
            int userId, string? q, bool? active, CancellationToken ct)
            => _repo.FindAsync(
                c =>
                    c.AppUserId == userId &&
                    (string.IsNullOrWhiteSpace(q) ||
                        c.Name.Contains(q) ||
                        (c.Description != null && c.Description.Contains(q))) &&
                    (!active.HasValue || c.IsActive == active),
                asNoTracking: true,
                ct: ct);


        // ------------------------------
        // GET
        // ------------------------------
        public Task<Concept?> GetAsync(int userId, int id, CancellationToken ct)
            => _repo.FirstOrDefaultAsync(
                c => c.AppUserId == userId && c.Id == id,
                asNoTracking: true,
                ct: ct);


        // ------------------------------
        // UPSERT (CREATE OR UPDATE)
        // ------------------------------
        public async Task<int> UpsertAsync(int userId, ConceptDetailDto dto, CancellationToken ct)
        {
            var name = dto.Name.Trim();

            // ----------------------
            // CREATE
            // ----------------------
            if (dto.Id == 0)
            {
                // UNIQUE CHECK: (AppUserId, Name)
                var exists = await _repo.AnyAsync(
                    c => c.AppUserId == userId && c.Name == name,
                    ct);

                if (exists)
                    throw new InvalidOperationException("Bu isimde bir konsept zaten mevcut.");

                var e = new Concept
                {
                    AppUserId = userId,
                    Name = name,
                    Description = dto.Description?.Trim(),
                    IsActive = dto.IsActive
                };

                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }

            // ----------------------
            // UPDATE
            // ----------------------
            else
            {
                var e = await _repo.FirstOrDefaultAsync(
                    c => c.AppUserId == userId && c.Id == dto.Id,
                    asNoTracking: false,
                    ct: ct);

                if (e is null)
                    throw new KeyNotFoundException("Konsept bulunamadı.");

                // Name değiştiyse UNIQUE kontrol
                if (!string.Equals(e.Name, name, StringComparison.Ordinal))
                {
                    var clash = await _repo.AnyAsync(
                        c => c.AppUserId == userId && c.Name == name && c.Id != dto.Id,
                        ct);

                    if (clash)
                        throw new InvalidOperationException("Bu isimde başka bir konsept var.");
                }

                e.Name = name;
                e.Description = dto.Description?.Trim();
                e.IsActive = dto.IsActive;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }


        // ------------------------------
        // DELETE (Soft Delete)
        // ------------------------------
        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                c => c.AppUserId == userId && c.Id == id,
                asNoTracking: false,
                ct: ct);

            if (e is null)
                return false;

            _repo.Delete(e); // Soft delete → Stamp() hallediyor
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
