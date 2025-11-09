using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ScriptGenerationProfileConfiguration : BaseEntityConfiguration<ScriptGenerationProfile>
    {
        public override void Configure(EntityTypeBuilder<ScriptGenerationProfile> builder)
        {
            base.Configure(builder);
            builder.ToTable("ScriptGenerationProfiles");


            // 🔹 Ana Alanlar
            builder.Property(e => e.ProfileName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.ModelName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValue("en");

            builder.Property(e => e.OutputMode)
                .HasMaxLength(20)
                .HasDefaultValue("Script");

            builder.Property(e => e.ConfigJson)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            builder.Property(e => e.ProductionType)
                .HasMaxLength(50);

            builder.Property(e => e.RenderStyle)
                .HasMaxLength(50);

            // 🔹 Numeric & Boolean Defaults
            builder.Property(e => e.Temperature)
                .HasDefaultValue(0.8f);

            builder.Property(e => e.IsPublic)
                .HasDefaultValue(false);

            builder.Property(e => e.AllowRetry)
                .HasDefaultValue(true);

            builder.Property(e => e.AutoGenerateAssets)
                .HasDefaultValue(false);

            builder.Property(e => e.AutoRenderVideo)
                .HasDefaultValue(false);

            // 🔹 Foreign Keys (User, Prompt, AiConnection)
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(e => e.Prompt)
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(e => e.AiConnection)
                .WithMany()
                .HasForeignKey(e => e.AiConnectionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(e => e.TopicGenerationProfile)
                .WithMany()
                .HasForeignKey(e => e.TopicGenerationProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            // 🔹 Image AI Relations
            builder.HasOne(e => e.ImageAiConnection)
                .WithMany()
                .HasForeignKey(e => e.ImageAiConnectionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(e => e.ImageModelName)
                .HasMaxLength(100);

            builder.Property(e => e.ImageRenderStyle)
                .HasMaxLength(50);

            builder.Property(e => e.ImageAspectRatio)
                .HasMaxLength(20)
                .HasDefaultValue("9:16");

            // 🔹 TTS AI Relations
            builder.HasOne(e => e.TtsAiConnection)
                .WithMany()
                .HasForeignKey(e => e.TtsAiConnectionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(e => e.TtsModelName)
                .HasMaxLength(100);

            builder.Property(e => e.TtsVoice)
                .HasMaxLength(50);

            // 🔹 Video AI Relations
            builder.HasOne(e => e.VideoAiConnection)
                .WithMany()
                .HasForeignKey(e => e.VideoAiConnectionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Property(e => e.VideoModelName)
                .HasMaxLength(100);

            builder.Property(e => e.VideoTemplate)
                .HasMaxLength(100);
        }
    }
}
