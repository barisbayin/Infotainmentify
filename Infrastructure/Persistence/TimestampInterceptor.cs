using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence
{
    public class TimestampInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return base.SavingChanges(eventData, result);
        }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            Stamp(eventData.Context);
            return base.SavingChangesAsync(eventData, result, ct);
        }

        private static void Stamp(DbContext? ctx)
        {
            if (ctx is null) return;
            var Now = DateTime.Now;

            foreach (var e in ctx.ChangeTracker.Entries<BaseEntity>())
            {
                if (e.State == EntityState.Added)
                    e.Entity.CreatedAt = Now;

                if (e.State == EntityState.Modified)
                    e.Entity.UpdatedAt = Now;

                // Eğer Delete çağrısında soft delete’e çevirmek istersen:
                if (e.State == EntityState.Deleted)
                {
                    e.State = EntityState.Modified;
                    e.Entity.Removed = true;
                    e.Entity.RemovedAt = Now;
                }
            }
        }
    }
}
