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

        public async Task<IReadOnlyList<Script>> ListAsync(
            int userId,
            int? topicId,
            string? q,
            CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: s =>
                    s.AppUserId == userId &&
                    (!topicId.HasValue || s.TopicId == topicId) &&
                    (string.IsNullOrWhiteSpace(q) || s.Title.Contains(q) || s.Content.Contains(q)),
                orderBy: s => s.CreatedAt,
                desc: true,
                include: src => src.Include(s => s.Topic), // TopicTitle için Join
                asNoTracking: true,
                ct: ct
            );
        }
    }
}
