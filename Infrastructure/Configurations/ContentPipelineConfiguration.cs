using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ContentPipelineConfiguration : BaseEntityConfiguration<ContentPipelineRun_>
    {
        public override void Configure(EntityTypeBuilder<ContentPipelineRun_> builder)
        {
            base.Configure(builder);

            builder.ToTable("ContentPipelines");

            // ------------ USER ------------
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ------------ PROFILE ------------
            builder.HasOne(x => x.Profile)
                .WithMany()
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // ------------ TOPIC ------------
            builder.HasOne(x => x.Topic)
                .WithMany()
                .HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.SetNull);

            // ------------ SCRIPT ------------
            builder.HasOne(x => x.Script)
                .WithMany()
                .HasForeignKey(x => x.ScriptId)
                .OnDelete(DeleteBehavior.SetNull);

            // ------------ SOCIAL CHANNEL ------------
            builder.HasOne(x => x.SocialChannel)
                .WithMany()
                .HasForeignKey(x => x.SocialChannelId)
                .OnDelete(DeleteBehavior.SetNull);

            // ------------ STRING FIELDS ------------
            builder.Property(x => x.ImagePathsJson)
                .HasMaxLength(4000);

            builder.Property(x => x.AudioPathsJson)
                .HasMaxLength(4000);

            builder.Property(x => x.VideoPath)
                .HasMaxLength(500);

            builder.Property(x => x.ThumbnailPath)
                .HasMaxLength(500);

            builder.Property(x => x.FinalTitle)
                .HasMaxLength(300);

            builder.Property(x => x.FinalDescription)
                .HasMaxLength(2000);

            builder.Property(x => x.UploadedVideoId)
                .HasMaxLength(200);

            builder.Property(x => x.UploadedPlatform)
                .HasMaxLength(50);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(500);

            builder.Property(x => x.LogJson)
                .HasMaxLength(4000);

            // ------------ ENUM ------------
            builder.Property(x => x.Status)
                .HasConversion<int>()       // Enum'ı int olarak sakla
                .IsRequired();
        }
    }
}
