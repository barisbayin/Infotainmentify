using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class TopicGenerationProfileConfiguration : BaseEntityConfiguration<TopicGenerationProfile>, IEntityTypeConfiguration<TopicGenerationProfile>
    {
        public override void Configure(EntityTypeBuilder<TopicGenerationProfile> builder)
        {
            base.Configure(builder);

            builder.ToTable("TopicGenerationProfiles");

            builder.Property(x => x.ProfileName)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

            builder.Property(x => x.AppUserId)
                   .IsRequired()
                   .HasColumnType("int");

            builder.Property(x => x.PromptId)
                   .IsRequired()
                   .HasColumnType("int");

            builder.Property(x => x.AiConnectionId)
                   .IsRequired()
                   .HasColumnType("int");

            builder.Property(x => x.ModelName)
                   .IsRequired()
                   .HasMaxLength(50)
                   .HasColumnType("nvarchar(50)");

            builder.Property(x => x.RequestedCount)
                   .HasColumnType("int");

            builder.Property(x => x.RawResponseJson)
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.StartedAt)
                   .IsRequired()
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.CompletedAt)
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.Status)
                   .HasMaxLength(50)
                   .HasColumnType("nvarchar(50)")
                   .HasDefaultValue("Pending");

            builder.Property(e => e.ProductionType)
                   .HasMaxLength(32);

            builder.Property(e => e.RenderStyle)
                   .HasMaxLength(64);

            // ilişkiler
            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.AppUserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Prompt)
                   .WithMany()
                   .HasForeignKey(x => x.PromptId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AiConnection)
                   .WithMany()
                   .HasForeignKey(x => x.AiConnectionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // indeksler
            builder.HasIndex(x => x.AppUserId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.StartedAt);
        }
    }
}
