using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class VideoGenerationProfileConfiguration
        : BaseEntityConfiguration<VideoGenerationProfile>
    {
        public override void Configure(EntityTypeBuilder<VideoGenerationProfile> b)
        {
            base.Configure(b);
            // 👆 BURASI ÇOK ÖNEMLİ — BaseEntity fields (CreatedAt, UpdatedAt, Removed, IsActive vb.)
            // hepsi burada configure oluyor.

            b.ToTable("VideoGenerationProfiles");

            // ---- Properties ----
            b.Property(x => x.ProfileName)
                .IsRequired()
                .HasMaxLength(100);

            b.Property(x => x.TitleTemplate)
                .HasMaxLength(200);

            b.Property(x => x.DescriptionTemplate)
                .HasMaxLength(2000);

            b.Property(x => x.UploadAfterRender)
                .IsRequired();

            b.Property(x => x.GenerateThumbnail)
                .IsRequired();

            // ---- Relationships ----

            // AppUser
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ScriptGenerationProfile
            b.HasOne(x => x.ScriptGenerationProfile)
                .WithMany()
                .HasForeignKey(x => x.ScriptGenerationProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // SocialChannel (nullable FK)
            b.HasOne(x => x.SocialChannel)
                .WithMany()
                .HasForeignKey(x => x.SocialChannelId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
