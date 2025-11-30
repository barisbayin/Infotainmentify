using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class TopicGenerationProfileConfiguration : BaseEntityConfiguration<TopicGenerationProfile>
    {
        public override void Configure(EntityTypeBuilder<TopicGenerationProfile> builder)
        {
            base.Configure(builder);
            builder.ToTable("TopicGenerationProfiles");

            // 🔹 Alan uzunlukları
            builder.Property(e => e.ProfileName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.ModelName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Language)
                .HasMaxLength(10)
                .HasDefaultValue("en")
                .IsRequired();

            builder.Property(e => e.ProductionType)
                .HasMaxLength(50);

            builder.Property(e => e.RenderStyle)
                .HasMaxLength(50);

            builder.Property(e => e.OutputMode)
                .HasMaxLength(20)
                .HasDefaultValue("Topic")
                .IsRequired();

            // 🔹 Sayısal alanlar
            builder.Property(e => e.RequestedCount)
                .HasDefaultValue(30);

            builder.Property(e => e.Temperature)
                .HasDefaultValue(0.7f);

            // 🔹 JSON ve bayraklar
            builder.Property(e => e.TagsJson)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.IsPublic)
                .HasDefaultValue(false);

            builder.Property(e => e.AllowRetry)
                .HasDefaultValue(true);

            // 🔹 İlişkiler
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Prompt)
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AiConnection)
                .WithMany()
                .HasForeignKey(e => e.AiConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
