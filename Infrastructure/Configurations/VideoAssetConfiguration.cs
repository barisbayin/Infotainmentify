using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class VideoAssetConfiguration : BaseEntityConfiguration<VideoAsset>
    {
        public override void Configure(EntityTypeBuilder<VideoAsset> builder)
        {
            base.Configure(builder); // 🧱 BaseEntity alanlarını uygula

            builder.ToTable("VideoAssets");

            // 🔗 Foreign keys
            builder.HasOne(x => x.Script)
                   .WithMany()
                   .HasForeignKey(x => x.ScriptId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 🧩 Property configs
            builder.Property(x => x.AssetType)
                   .HasMaxLength(64)
                   .IsRequired();

            builder.Property(x => x.AssetKey)
                   .HasMaxLength(128)
                   .IsRequired();

            builder.Property(x => x.FilePath)
                   .HasMaxLength(256)
                   .IsRequired();

            builder.Property(x => x.IsGenerated)
                   .HasDefaultValue(false);

            builder.Property(x => x.IsUploaded)
                   .HasDefaultValue(false);

            builder.Property(x => x.GeneratedAt)
                   .HasColumnType("datetime2");

            builder.Property(x => x.UploadedAt)
                   .HasColumnType("datetime2");

            builder.Property(x => x.MetadataJson)
                   .HasColumnType("nvarchar(max)");

            // 📈 Indexes
            builder.HasIndex(x => new { x.ScriptId, x.AssetType });
            builder.HasIndex(x => x.UserId);
        }
    }
}
