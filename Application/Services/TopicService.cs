using Application.Contracts.Topics;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    public class TopicService
    {
        private readonly IRepository<Topic> _repo;
        private readonly IUnitOfWork _uow;

        public TopicService(IRepository<Topic> repo, IUnitOfWork uow)
        { _repo = repo; _uow = uow; }

        // List: user-scoped + basit filtreler (q -> code/premise)
        public Task<IReadOnlyList<Topic>> ListAsync(
            int userId, string? q, string? category, CancellationToken ct)
            => _repo.FindAsync(t =>
                   t.UserId == userId &&
                   (string.IsNullOrWhiteSpace(q)
                        || (t.TopicCode != null && t.TopicCode.Contains(q))
                        || (t.Premise != null && t.Premise.Contains(q))
                        || (t.PremiseTr != null && t.PremiseTr.Contains(q))) &&
                   (string.IsNullOrWhiteSpace(category) || t.Category == category),
               asNoTracking: true, ct);

        public Task<Topic?> GetAsync(int userId, int id, CancellationToken ct)
            => _repo.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id, asNoTracking: true, ct: ct);

        /// <summary>Id == 0 → Create, Id > 0 → Update. (UserId, TopicCode) benzersiz kabul edilirse kontrol içerir.</summary>
        public async Task<int> UpsertAsync(int userId, TopicDetailDto dto, CancellationToken ct)
        {
            var code = dto.TopicCode?.Trim();
            if (dto.Id == 0)
            {
                if (!string.IsNullOrEmpty(code))
                {
                    var exists = await _repo.AnyAsync(t => t.UserId == userId && t.TopicCode == code, ct);
                    if (exists) throw new InvalidOperationException("Bu TopicCode zaten kullanılıyor.");
                }

                var e = new Topic
                {
                    UserId = userId,
                    TopicCode = code,
                    Category = dto.Category?.Trim(),
                    PremiseTr = dto.PremiseTr,
                    Premise = dto.Premise,
                    Tone = dto.Tone,
                    PotentialVisual = dto.PotentialVisual,
                    NeedsFootage = dto.NeedsFootage,
                    FactCheck = dto.FactCheck,
                    TagsJson = dto.TagsJson,
                    TopicJson = dto.TopicJson,
                    PromptId = dto.PromptId
                };
                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
            else
            {
                var e = await _repo.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == dto.Id, asNoTracking: false, ct: ct);
                if (e is null) throw new KeyNotFoundException("Topic bulunamadı.");

                // code değişiyorsa benzersizliğe bak
                if (!string.Equals(e.TopicCode, code, StringComparison.Ordinal) && !string.IsNullOrEmpty(code))
                {
                    var clash = await _repo.AnyAsync(t => t.UserId == userId && t.TopicCode == code && t.Id != dto.Id, ct);
                    if (clash) throw new InvalidOperationException("Bu TopicCode zaten kullanılıyor.");
                }

                e.TopicCode = code;
                e.Category = dto.Category?.Trim();
                e.PremiseTr = dto.PremiseTr;
                e.Premise = dto.Premise;
                e.Tone = dto.Tone;
                e.PotentialVisual = dto.PotentialVisual;
                e.NeedsFootage = dto.NeedsFootage;
                e.FactCheck = dto.FactCheck;
                e.TagsJson = dto.TagsJson;
                e.TopicJson = dto.TopicJson;
                e.PromptId = dto.PromptId;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id, asNoTracking: false, ct: ct);
            if (e is null) return false;

            // Soft delete: DbContext.Stamp() hallediyor.
            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
