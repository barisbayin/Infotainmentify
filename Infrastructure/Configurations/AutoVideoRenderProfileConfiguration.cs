using Core.Entity;
using Core.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AutoVideoRenderProfileConfiguration : BaseEntityConfiguration<AutoVideoRenderProfile>, IEntityTypeConfiguration<AutoVideoRenderProfile>
    {
        public override void Configure(EntityTypeBuilder<AutoVideoRenderProfile> b)
        {
            b.ToTable("AutoVideoRenderProfiles");

            b.HasKey(x => x.Id);

            b.Property(x => x.Resolution).HasMaxLength(16);
            b.Property(x => x.Style).HasMaxLength(64);
            b.Property(x => x.CaptionStyle).HasMaxLength(64);
            b.Property(x => x.CaptionFont).HasMaxLength(64);
            b.Property(x => x.Transition).HasMaxLength(32);
            b.Property(x => x.TimelineMode).HasMaxLength(32);

            b.Property(x => x.AiRecommendedStyle).HasMaxLength(64);
            b.Property(x => x.AiRecommendedTransitions).HasMaxLength(64);
            b.Property(x => x.AiRecommendedCaption).HasMaxLength(64);

            b.Property(x => x.CaptionPosition)
             .HasConversion<int>()
             .IsRequired();

            b.Property(x => x.CaptionAnimation)
             .HasConversion<int>()
             .IsRequired();

            b.HasOne<AppUser>()
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
