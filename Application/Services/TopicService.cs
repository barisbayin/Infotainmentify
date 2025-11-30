using Application.Contracts.Mappers;
using Application.Contracts.Topics;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class TopicService : BaseService<Topic>
    {
        public TopicService(IRepository<Topic> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }

        // Özelleştirilmiş Listeleme (Arama ve Filtreleme)
        public async Task<IReadOnlyList<Topic>> ListAsync(
            int userId,
            string? q,
            string? category,
            CancellationToken ct)
        {
            // Repository FindAsync metodunu kullanıyoruz
            return await _repo.FindAsync(
                predicate: t =>
                    t.AppUserId == userId &&
                    (string.IsNullOrWhiteSpace(q) ||
                     t.Title.Contains(q) ||
                     t.Premise.Contains(q)) &&
                    (string.IsNullOrWhiteSpace(category) || t.Category == category),
                orderBy: t => t.CreatedAt,
                desc: true, // En yeni en üstte
                asNoTracking: true,
                ct: ct
            );
        }
    }
}