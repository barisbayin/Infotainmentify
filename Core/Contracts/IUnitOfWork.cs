using Microsoft.EntityFrameworkCore;

namespace Core.Contracts
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
        DbContext GetDbContext();
    }

}
