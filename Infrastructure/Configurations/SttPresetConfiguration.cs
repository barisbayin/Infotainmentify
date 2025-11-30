using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class SttPresetConfiguration : BaseEntityConfiguration<SttPreset>, IEntityTypeConfiguration<SttPreset>
    {
        public override void Configure(EntityTypeBuilder<SttPreset> b)
        {
            base.Configure(b);
            b.ToTable("SttPresets");

            // İsim çakışması önleme
            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
            b.Property(x => x.OutputFormat).HasMaxLength(10).IsRequired();

            b.Property(x => x.Prompt).HasMaxLength(1000);

            // İlişkiler
            b.HasOne(x => x.AppUser)
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // Connection silinirse preset kullanılamaz, o yüzden bağlantıyı koruyoruz.
            b.HasOne(x => x.UserAiConnection)
             .WithMany()
             .HasForeignKey(x => x.UserAiConnectionId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
