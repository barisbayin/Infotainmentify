using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Infrastructure.Persistence
{
    public class EfRepository<TEntity> : IRepository<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        // Soft delete’i otomatik filtrelemek istiyorsan true yap
        private readonly bool _applySoftDeleteFilter = true;

        public EfRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        protected virtual IQueryable<TEntity> BaseQuery(bool asNoTracking = true)
        {
            IQueryable<TEntity> q = _dbSet;
            if (asNoTracking) q = q.AsNoTracking();
            if (_applySoftDeleteFilter && typeof(TEntity).GetProperty(nameof(BaseEntity.Removed)) != null)
                q = q.Where(e => !EF.Property<bool>(e, nameof(BaseEntity.Removed)));
            return q;
        }

        protected static IQueryable<TEntity> ApplyIncludes(
            IQueryable<TEntity> q,
            params Expression<Func<TEntity, object>>[] includes)
        {
            if (includes == null) return q;
            foreach (var inc in includes)
                q = q.Include(inc);
            return q;
        }

        public async Task<TEntity?> GetByIdAsync(int id, bool asNoTracking = true, CancellationToken ct = default)
            => await BaseQuery(asNoTracking).FirstOrDefaultAsync(e => e.Id == id, ct);

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(
            bool asNoTracking = true, CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var q = ApplyIncludes(BaseQuery(asNoTracking), includes);
            return await q.ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var q = ApplyIncludes(BaseQuery(asNoTracking).Where(predicate), includes);
            return await q.ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TEntity>> FindAsync<TOrderKey>(
             Expression<Func<TEntity, bool>> predicate,
             Expression<Func<TEntity, TOrderKey>> orderBy,
             bool desc = true,
             Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
             bool asNoTracking = true,
             CancellationToken ct = default)
        {
            IQueryable<TEntity> query = _dbSet;

            // filtre
            query = query.Where(predicate);

            // include
            if (include != null)
                query = include(query);

            // sıralama
            query = desc ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            // tracking opsiyonu
            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync(ct);
        }



        public async Task<IReadOnlyList<TEntity>> FindAsync<TOrderKey>(
    Expression<Func<TEntity, bool>> predicate,
    Expression<Func<TEntity, TOrderKey>> orderBy,
    bool desc = true,
    bool asNoTracking = true,
    CancellationToken ct = default,
    params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var include in includes)
                query = query.Include(include);

            query = query.Where(predicate);

            query = desc
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync(ct);
        }


        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var q = ApplyIncludes(BaseQuery(asNoTracking).Where(predicate), includes);
            return await q.FirstOrDefaultAsync(ct);
        }


        public async Task<IReadOnlyList<TEntity>> FindAsync(
    Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool asNoTracking = true,
    CancellationToken ct = default)
        {
            IQueryable<TEntity> query = _dbSet;

            if (asNoTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            query = query.Where(predicate);

            return await query.ToListAsync(ct);
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
            bool asNoTracking = true,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = _dbSet;

            if (asNoTracking)
                query = query.AsNoTracking();

            if (include != null)
                query = include(query);

            return await query.FirstOrDefaultAsync(predicate, ct);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
            => BaseQuery(asNoTracking: true).AnyAsync(predicate, ct);

        public async Task<(IReadOnlyList<TEntity> Items, int Total)> GetPagedAsync<TOrderKey>(
            Expression<Func<TEntity, bool>>? predicate,
            Expression<Func<TEntity, TOrderKey>>? orderBy,
            bool desc = true,
            int page = 1,
            int pageSize = 20,
            bool asNoTracking = true,
            CancellationToken ct = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var q = BaseQuery(asNoTracking);
            if (predicate != null) q = q.Where(predicate);
            q = ApplyIncludes(q, includes);

            var total = await q.CountAsync(ct);

            if (orderBy != null)
                q = desc ? q.OrderByDescending(orderBy) : q.OrderBy(orderBy);

            var items = await q.Skip(Math.Max(0, (page - 1) * pageSize))
                               .Take(Math.Clamp(pageSize, 1, 200))
                               .ToListAsync(ct);

            return (items, total);
        }

        public Task AddAsync(TEntity entity, CancellationToken ct = default)
            => _dbSet.AddAsync(entity, ct).AsTask();

        public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
            => _dbSet.AddRangeAsync(entities, ct);

        public void Update(TEntity entity)
            => _dbSet.Update(entity);

        public void UpdateRange(IEnumerable<TEntity> entities)
            => _dbSet.UpdateRange(entities);

        public void Delete(TEntity entity)
        {
            // Soft delete:
            var removedProp = typeof(TEntity).GetProperty(nameof(BaseEntity.Removed));
            var removedAtProp = typeof(TEntity).GetProperty(nameof(BaseEntity.RemovedAt));

            if (_applySoftDeleteFilter && removedProp != null && removedAtProp != null)
            {
                removedProp.SetValue(entity, true);
                removedAtProp.SetValue(entity, DateTime.Now);
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _context.SaveChangesAsync(ct);

        public async Task DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        {
            // Bu komut SQL'e direkt "DELETE FROM Table WHERE ..." gönderir.
            // Change Tracker'a takılmaz, çok hızlıdır ve o hatayı almazsın.
            await _dbSet
                .Where(predicate)
                .ExecuteDeleteAsync(ct);
        }
    }
}
