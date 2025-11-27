using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AutoVideoAssetFileConfiguration: BaseEntityConfiguration<AutoVideoAssetFile>
    {
        public override void Configure(EntityTypeBuilder<AutoVideoAssetFile> b)
        {
            base.Configure(b);

            // ---- USER ----
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- PIPELINE ----
            b.HasOne(x => x.Pipeline)
                .WithMany()
                .HasForeignKey(x => x.AutoVideoPipelineId)
                .OnDelete(DeleteBehavior.Cascade); // Pipeline silinirse assetler de silinsin

            // ---- STRING FIELDS ----

            b.Property(x => x.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            b.Property(x => x.AssetKey)
                .HasMaxLength(100);

            b.Property(x => x.MetadataJson)
                .HasMaxLength(4000);

            // ---- ENUM ----
            b.Property(x => x.FileType)
                .HasConversion<int>()
                .IsRequired();

            // ---- SceneNumber ----
            b.Property(x => x.SceneNumber)
                .IsRequired();
        }
    }
}
