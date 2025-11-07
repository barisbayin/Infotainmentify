using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ScriptGenerationProfileConfiguration : IEntityTypeConfiguration<ScriptGenerationProfile>
    {
        public void Configure(EntityTypeBuilder<ScriptGenerationProfile> b)
        {
            b.ToTable("ScriptGenerationProfiles");

            b.HasKey(x => x.Id);

            b.Property(x => x.ProfileName)
                .HasMaxLength(100)
                .IsRequired();

            b.Property(x => x.ModelName)
                .HasMaxLength(50)
                .IsRequired();

            b.Property(x => x.Temperature)
                .HasColumnType("float")
                .HasDefaultValue(0.8);

            b.Property(x => x.Language)
                .HasMaxLength(10)
                .HasDefaultValue("en");

            b.Property(x => x.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            b.Property(x => x.TopicIdsJson)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.ConfigJson)
                .HasColumnType("nvarchar(max)");

            b.Property(x => x.RawResponseJson)
                .HasColumnType("nvarchar(max)");

            b.HasOne(x => x.Prompt)
                .WithMany()
                .HasForeignKey(x => x.PromptId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.AiConnection)
                .WithMany()
                .HasForeignKey(x => x.AiConnectionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.TopicGenerationProfile)
                .WithMany()
                .HasForeignKey(x => x.TopicGenerationProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
