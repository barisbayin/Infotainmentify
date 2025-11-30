using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class TtsPresetConfiguration : BaseEntityConfiguration<TtsPreset>, IEntityTypeConfiguration<TtsPreset>
    {
        public override void Configure(EntityTypeBuilder<TtsPreset> b)
        {
            base.Configure(b);
            b.ToTable("TtsPresets");

            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.VoiceId).HasMaxLength(100).IsRequired();
            b.Property(x => x.LanguageCode).HasMaxLength(20).IsRequired();
            b.Property(x => x.EngineModel).HasMaxLength(100);

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
