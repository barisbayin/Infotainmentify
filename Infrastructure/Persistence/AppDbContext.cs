using Core.Abstractions;
using Core.Entity;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Prompt> Prompts => Set<Prompt>();
        public DbSet<Topic> Topics => Set<Topic>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tüm IEntityTypeConfiguration<> sınıflarını YERİNDEN yükle
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Eğer konfigurasyonlar Infrastructure.Persistence.Configurations içindeyse:
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppUserConfiguration).Assembly);

            // Ekstra güvence istersen:
            // modelBuilder.Entity<AppUser>(); // DbSet olsa da olmasa da explicit discover

            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clr = entityType.ClrType;
                if (typeof(ISoftDelete).IsAssignableFrom(clr))
                {
                    var param = Expression.Parameter(clr, "e");
                    var prop = Expression.Property(param, nameof(ISoftDelete.Removed));
                    var body = Expression.Equal(prop, Expression.Constant(false));
                    var lambda = Expression.Lambda(body, param);

                    modelBuilder.Entity(clr).HasQueryFilter(lambda);
                }
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            Stamp();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
        {
            Stamp();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
        }
        private void Stamp()
        {
            var now = DateTime.UtcNow;
            foreach (var e in ChangeTracker.Entries<BaseEntity>())
            {
                if (e.State == EntityState.Added) e.Entity.CreatedAt = now;
                if (e.State == EntityState.Modified) e.Entity.UpdatedAt = now;
                if (e.State == EntityState.Deleted) { e.State = EntityState.Modified; e.Entity.Removed = true; e.Entity.RemovedAt = now; }
            }
        }
    }
}
