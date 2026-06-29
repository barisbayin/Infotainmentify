using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class SavedProductionBriefConfiguration : BaseEntityConfiguration<SavedProductionBrief>, IEntityTypeConfiguration<SavedProductionBrief>
    {
        public override void Configure(EntityTypeBuilder<SavedProductionBrief> b)
        {
            base.Configure(b);

            b.ToTable("SavedProductionBriefs");

            b.HasIndex(x => new { x.AppUserId, x.Name });
            b.HasIndex(x => new { x.AppUserId, x.ConceptId });
            b.HasIndex(x => x.LastUsedAt);

            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
            b.Property(x => x.MainTitle).HasMaxLength(300);
            b.Property(x => x.Angle).HasMaxLength(1000);
            b.Property(x => x.Audience).HasMaxLength(500);
            b.Property(x => x.TargetDuration).HasMaxLength(100);
            b.Property(x => x.MustCover).HasMaxLength(2500);
            b.Property(x => x.Avoid).HasMaxLength(1500);
            b.Property(x => x.Notes).HasMaxLength(2500);
            b.Property(x => x.LastUsedAt).HasColumnType("datetime2");

            b.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne(x => x.Concept)
                .WithMany()
                .HasForeignKey(x => x.ConceptId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
