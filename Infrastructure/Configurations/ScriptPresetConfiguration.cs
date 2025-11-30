using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ScriptPresetConfiguration : BaseEntityConfiguration<ScriptPreset>, IEntityTypeConfiguration<ScriptPreset>
    {
        public override void Configure(EntityTypeBuilder<ScriptPreset> b)
        {
            base.Configure(b);
            b.ToTable("ScriptPresets");

            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.PromptTemplate).HasMaxLength(5000).IsRequired();

            b.Property(x => x.Tone).HasMaxLength(50).IsRequired();
            b.Property(x => x.Language).HasMaxLength(10).IsRequired();

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
