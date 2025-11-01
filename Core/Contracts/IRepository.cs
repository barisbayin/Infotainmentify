using Core.Entity;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;


namespace Core.Contracts
{
    public interface IRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(int id, bool asNoTracking = true, CancellationToken ct = default);

        Task<IReadOnlyList<TEntity>> GetAllAsync(
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes);

        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes);

        Task<IReadOnlyList<TEntity>> FindAsync<TOrderKey>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TOrderKey>> orderBy,
            bool desc = true,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes);

        Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes);


        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

        // Paging + order destekli listeleme
        Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync<TOrderKey>(
            Expression<Func<TEntity, bool>>? predicate,
            Expression<Func<TEntity, TOrderKey>>? orderBy,
            bool desc = true,
            int page = 1,
            int pageSize = 20,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes);

        Task AddAsync(TEntity entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
        void Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);
        void Delete(TEntity entity);

        // İstersen burada kalsın; çoğu mimaride UoW'a taşınır.
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
