using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AutoVideoAssetConfiguration
        : BaseEntityConfiguration<AutoVideoAsset>
    {
        public override void Configure(EntityTypeBuilder<AutoVideoAsset> b)
        {
            base.Configure(b);

            // -------------------------
            // Table
            // -------------------------
            b.ToTable("AutoVideoAssets");

            // -------------------------
            // Properties
            // -------------------------

            b.Property(x => x.VideoPath)
                .HasMaxLength(500);

            b.Property(x => x.ThumbnailPath)
                .HasMaxLength(500);

            b.Property(x => x.UploadVideoId)
                .HasMaxLength(200);

            b.Property(x => x.UploadPlatform)
                .HasMaxLength(50);

            b.Property(x => x.Log)
                .HasColumnType("nvarchar(max)");

            // -------------------------
            // Relationships
            // -------------------------

            // User
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // AutoVideoAssetProfile
            b.HasOne<AutoVideoAssetProfile>()
                .WithMany()
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            // Topic
            b.HasOne<Topic>()
                .WithMany()
                .HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.NoAction);

            // Script
            b.HasOne<Script>()
                .WithMany()
                .HasForeignKey(x => x.ScriptId)
                .OnDelete(DeleteBehavior.NoAction);

            // -------------------------
            // Indexes
            // -------------------------

            // Kullanıcı bazlı görüntüleme
            b.HasIndex(x => x.AppUserId);

            // Profil bazlı görüntüleme / pipeline için önemli
            b.HasIndex(x => x.ProfileId);

            // Status ile arama (dashboard gibi)
            b.HasIndex(x => x.Status);

            // Topic & Script optional, ama index iyi performans getirir
            b.HasIndex(x => x.TopicId);
            b.HasIndex(x => x.ScriptId);

            // Upload search
            b.HasIndex(x => x.UploadPlatform);
            b.HasIndex(x => x.UploadVideoId);
        }
    }
}
