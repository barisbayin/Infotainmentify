using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ImagePresetConfiguration : BaseEntityConfiguration<ImagePreset>, IEntityTypeConfiguration<ImagePreset>
    {
        public override void Configure(EntityTypeBuilder<ImagePreset> b)
        {
            base.Configure(b);
            b.ToTable("ImagePresets");

            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.Size).HasMaxLength(20).IsRequired();

            b.Property(x => x.PromptTemplate).HasMaxLength(5000).IsRequired();
            b.Property(x => x.NegativePrompt).HasMaxLength(2000);

            // İlişkiler
            b.HasOne(x => x.AppUser)
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.UserAiConnection)
             .WithMany()
             .HasForeignKey(x => x.UserAiConnectionId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
