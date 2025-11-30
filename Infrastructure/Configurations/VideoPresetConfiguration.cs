using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configurations
{
    public sealed class VideoPresetConfiguration : BaseEntityConfiguration<VideoPreset>, IEntityTypeConfiguration<VideoPreset>
    {
        public override void Configure(EntityTypeBuilder<VideoPreset> b)
        {
            base.Configure(b);
            b.ToTable("VideoPresets");

            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.AspectRatio).HasMaxLength(20).IsRequired();
            b.Property(x => x.PromptTemplate).HasMaxLength(5000).IsRequired();

            // Enum
            b.Property(x => x.GenerationMode).HasConversion<int>().IsRequired();

            // JSON Alanları
            b.Property(x => x.CameraControlSettingsJson).IsRequired(false);
            b.Property(x => x.AdvancedSettingsJson).IsRequired(false);

            // İlişkiler
            b.HasOne(x => x.AppUser)
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.UserAiConnection)
             .WithMany()
             .HasForeignKey(x => x.UserAiConnectionId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
