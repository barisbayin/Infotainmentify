using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ProductionConceptProfileConfiguration : BaseEntityConfiguration<ProductionConceptProfile>, IEntityTypeConfiguration<ProductionConceptProfile>
    {
        public override void Configure(EntityTypeBuilder<ProductionConceptProfile> b)
        {
            base.Configure(b);

            b.ToTable("ProductionConceptProfiles");

            b.HasIndex(x => new { x.AppUserId, x.ConceptId }).IsUnique();
            b.HasIndex(x => x.ProductionProfile);
            b.HasIndex(x => x.DefaultTemplateId);

            b.Property(x => x.ProductionProfile).HasMaxLength(40).HasDefaultValue("LongForm").IsRequired();
            b.Property(x => x.DefaultLanguage).HasMaxLength(20).HasDefaultValue("en-US").IsRequired();
            b.Property(x => x.DefaultPlatform).HasMaxLength(60).HasDefaultValue("YouTube").IsRequired();
            b.Property(x => x.Audience).HasMaxLength(800);
            b.Property(x => x.Tone).HasMaxLength(800);
            b.Property(x => x.ChannelPromise).HasMaxLength(1200);
            b.Property(x => x.VisualStyleName).HasMaxLength(160);
            b.Property(x => x.VisualStyleBible).HasColumnType("nvarchar(max)");
            b.Property(x => x.CharacterBible).HasColumnType("nvarchar(max)");
            b.Property(x => x.TextPolicy).HasColumnType("nvarchar(max)");
            b.Property(x => x.ContentRules).HasColumnType("nvarchar(max)");
            b.Property(x => x.DefaultReviewPolicyJson).HasColumnType("nvarchar(max)");

            b.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();

            b.HasOne(x => x.Concept)
                .WithOne(x => x.Profile)
                .HasForeignKey<ProductionConceptProfile>(x => x.ConceptId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne(x => x.DefaultTemplate)
                .WithMany()
                .HasForeignKey(x => x.DefaultTemplateId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
