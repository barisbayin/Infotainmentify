using Core.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _ctx;
        public EfUnitOfWork(AppDbContext ctx) => _ctx = ctx;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            await using var tx = await _ctx.Database.BeginTransactionAsync(ct);
            await action();
            await _ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        public DbContext GetDbContext() => _ctx;
    }
}
