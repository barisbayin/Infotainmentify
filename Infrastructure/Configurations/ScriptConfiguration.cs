using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ScriptConfiguration : BaseEntityConfiguration<Script>
    {
        public override void Configure(EntityTypeBuilder<Script> builder)
        {
            base.Configure(builder);
            builder.ToTable("Scripts");

            // 🔹 Temel alanlar
            builder.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Content)
                .IsRequired();

            builder.Property(e => e.Summary)
                .HasMaxLength(200);

            builder.Property(e => e.Language)
                .HasMaxLength(50);

            builder.Property(e => e.RenderStyle)
                .HasMaxLength(50);

            builder.Property(e => e.ProductionType)
                .HasMaxLength(50);

            // 🔹 JSON alanları
            builder.Property(e => e.MetaJson)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.ScriptJson)
                .HasColumnType("nvarchar(max)");

            // 🔹 İlişkiler
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(e => e.Topic)
                .WithMany()
                .HasForeignKey(e => e.TopicId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(e => e.Prompt)
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.AiConnection)
                .WithMany()
                .HasForeignKey(e => e.AiConnectionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.ScriptGenerationProfile)
                .WithMany()
                .HasForeignKey(e => e.ScriptGenerationProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🔹 Performans ölçüm alanı
            builder.Property(e => e.ResponseTimeMs)
                .HasColumnType("int");

            // 🔹 Indexler
            builder.HasIndex(e => e.TopicId);
            builder.HasIndex(e => e.UserId);
            builder.HasIndex(e => e.PromptId);
            builder.HasIndex(e => e.AiConnectionId);
            builder.HasIndex(e => e.ScriptGenerationProfileId);
        }
    }
}
