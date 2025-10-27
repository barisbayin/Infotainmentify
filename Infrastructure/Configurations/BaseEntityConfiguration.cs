using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
            where T : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(e => e.Id);

            // Timestamp'ler: SQL Server -> datetime2(0) + GETUTCDATE()
            builder.Property(e => e.CreatedAt)
                   .HasColumnType("datetime2(0)")
                   .HasDefaultValueSql("GETUTCDATE()")
                   .IsRequired();

            builder.Property(e => e.UpdatedAt)
                   .HasColumnType("datetime2(0)")
                   .IsRequired(false);

            // Soft delete alanları
            builder.Property(e => e.Removed)
                   .HasColumnType("bit")
                   .HasDefaultValue(false);

            builder.Property(e => e.RemovedAt)
                   .HasColumnType("datetime2(0)")
                   .IsRequired(false);

            // Faydalı index'ler
            builder.HasIndex(e => e.CreatedAt);
            builder.HasIndex(e => e.Removed);

            // Global query filter (soft delete)
            builder.HasQueryFilter(e => !e.Removed);
        }
    }
}
