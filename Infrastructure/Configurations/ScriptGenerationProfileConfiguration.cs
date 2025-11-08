using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ScriptGenerationProfileConfiguration : IEntityTypeConfiguration<ScriptGenerationProfile>
    {
        public void Configure(EntityTypeBuilder<ScriptGenerationProfile> builder)
        {
            builder.ToTable("ScriptGenerationProfiles");

            builder.Property(e => e.ProfileName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.ModelName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValue("en");

            builder.Property(e => e.OutputMode)
                .HasMaxLength(20)
                .HasDefaultValue("Script");

            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            builder.Property(e => e.ProductionType)
                .HasMaxLength(50);

            builder.Property(e => e.RenderStyle)
                .HasMaxLength(50);

            builder.Property(e => e.IsPublic)
                .HasDefaultValue(false);

            builder.Property(e => e.AllowRetry)
                .HasDefaultValue(true);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AiConnection)
                .WithMany()
                .HasForeignKey(e => e.AiConnectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Prompt)
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.TopicGenerationProfile)
                .WithMany()
                .HasForeignKey(e => e.TopicGenerationProfileId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
