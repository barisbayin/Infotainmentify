using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class RenderPresetConfiguration : BaseEntityConfiguration<RenderPreset>, IEntityTypeConfiguration<RenderPreset>
    {
        public override void Configure(EntityTypeBuilder<RenderPreset> b)
        {
            base.Configure(b);
            b.ToTable("RenderPresets");

            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();

            // JSON Kolonları (Limitsiz)
            b.Property(x => x.CaptionSettingsJson).IsRequired(false);
            b.Property(x => x.AudioMixSettingsJson).IsRequired(false);
            b.Property(x => x.VisualEffectsSettingsJson).IsRequired(false);
            b.Property(x => x.BrandingSettingsJson).IsRequired(false);

            // İlişkiler
            b.HasOne(x => x.AppUser)
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
