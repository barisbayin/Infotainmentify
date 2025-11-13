using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AutoVideoAssetProfileConfiguration
           : BaseEntityConfiguration<AutoVideoAssetProfile>
    {
        public override void Configure(EntityTypeBuilder<AutoVideoAssetProfile> b)
        {
            base.Configure(b); // <-- tüm ortak kolonlar burada set ediliyor

            // -------------------------
            // Table Name (opsiyonel)
            // -------------------------
            b.ToTable("AutoVideoAssetProfiles");

            // -------------------------
            // Properties
            // -------------------------
            b.Property(x => x.ProfileName)
                .IsRequired()
                .HasMaxLength(100);

            b.Property(x => x.TitleTemplate)
                .HasMaxLength(200);

            b.Property(x => x.DescriptionTemplate)
                .HasMaxLength(2000);

            // -------------------------
            // Relationships
            // -------------------------

            // AppUser
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Topic Profile
            b.HasOne(x => x.TopicGenerationProfile)
                .WithMany()
                .HasForeignKey(x => x.TopicGenerationProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            // Script Profile
            b.HasOne(x => x.ScriptGenerationProfile)
                .WithMany()
                .HasForeignKey(x => x.ScriptGenerationProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            // Social Channel
            b.HasOne(x => x.SocialChannel)
                .WithMany()
                .HasForeignKey(x => x.SocialChannelId)
                .OnDelete(DeleteBehavior.NoAction);

            // -------------------------
            // Indexes
            // -------------------------

            // Kullanıcı bazlı performans için
            b.HasIndex(x => x.AppUserId);

            // Her kullanıcı için profileName benzersiz
            b.HasIndex(x => new { x.AppUserId, x.ProfileName })
                .IsUnique();

            b.HasIndex(x => x.TopicGenerationProfileId);
            b.HasIndex(x => x.ScriptGenerationProfileId);
            b.HasIndex(x => x.SocialChannelId);
        }
    }
}
