using Application.Contracts.Prompts;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    public class PromptService
    {
        private readonly IRepository<Prompt> _repo;
        private readonly IUnitOfWork _uow;

        public PromptService(IRepository<Prompt> repo, IUnitOfWork uow)
        { _repo = repo; _uow = uow; }

        public Task<IReadOnlyList<Prompt>> ListAsync(
            int userId, string? q, string? category, bool? active, CancellationToken ct)
            => _repo.FindAsync(p =>
                   p.UserId == userId &&
                   (string.IsNullOrWhiteSpace(q) ||
                        p.Name.Contains(q) ||
                        p.Body.Contains(q) ||
                        (p.Description != null && p.Description.Contains(q))) &&
                   (string.IsNullOrWhiteSpace(category) || p.Category == category) &&
                   (!active.HasValue || p.IsActive == active),
               asNoTracking: true, ct);

        public Task<Prompt?> GetAsync(int userId, int id, CancellationToken ct)
            => _repo.FirstOrDefaultAsync(p => p.UserId == userId && p.Id == id, asNoTracking: true, ct: ct);

        /// <summary>
        /// Id == 0 → Create, Id > 0 → Update. Geriye kayıt Id’sini döner.
        /// (UserId, Name) benzersiz kontrolü içerir.
        /// </summary>
        public async Task<int> UpsertAsync(int userId, PromptDetailDto dto, CancellationToken ct)
        {
            var name = dto.Name.Trim();

            if (dto.Id == 0)
            {
                // UNIQUE: (UserId, Name)
                var exists = await _repo.AnyAsync(p => p.UserId == userId && p.Name == name, ct);
                if (exists) throw new InvalidOperationException("Bu isimde bir prompt zaten var.");

                var e = new Prompt
                {
                    UserId = userId,
                    Name = name,
                    Category = dto.Category?.Trim(),
                    Language = dto.Language?.Trim(),
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                    Body = dto.Body
                };
                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
            else
            {
                var e = await _repo.FirstOrDefaultAsync(p => p.UserId == userId && p.Id == dto.Id, asNoTracking: false, ct: ct);
                if (e is null) throw new KeyNotFoundException("Prompt bulunamadı.");

                if (!string.Equals(e.Name, name, StringComparison.Ordinal))
                {
                    var clash = await _repo.AnyAsync(p => p.UserId == userId && p.Name == name && p.Id != dto.Id, ct);
                    if (clash) throw new InvalidOperationException("Bu isimde başka bir prompt var.");
                }

                e.Name = name;
                e.Category = dto.Category?.Trim();
                e.Language = dto.Language?.Trim();
                e.Description = dto.Description;
                e.IsActive = dto.IsActive;
                e.Body = dto.Body;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(p => p.UserId == userId && p.Id == id, asNoTracking: false, ct: ct);
            if (e is null) return false;

            // Soft delete’i DbContext.Stamp() zaten hallediyor.
            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }

}
