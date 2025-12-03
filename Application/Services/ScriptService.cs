using Application.Services.Base;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ScriptService : BaseService<Script>
    {
        public ScriptService(IRepository<Script> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }

        // Filtreli Liste
        public async Task<IReadOnlyList<Script>> ListAsync(
            int userId,
            int? topicId,
            int? conceptId, // 🔥 YENİ: Konsept Filtresi
            string? q,
            CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: s =>
                    s.AppUserId == userId &&
                    (!topicId.HasValue || s.TopicId == topicId) &&
                    // 🔥 KRİTİK NOKTA: Konsepti, Topic üzerinden sorguluyoruz
                    (!conceptId.HasValue || s.Topic.ConceptId == conceptId) &&
                    (string.IsNullOrWhiteSpace(q) || s.Title.Contains(q) || s.Content.Contains(q)),

                orderBy: s => s.CreatedAt,
                desc: true,

                // Topic tablosunu Joinliyoruz ki ConceptId'ye erişebilelim
                include: src => src.Include(s => s.Topic),

                asNoTracking: true,
                ct: ct
            );
        }
    }
}
