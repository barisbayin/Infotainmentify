using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    /// <summary>
    /// Topic entity configuration
    /// İçerik üretim pipeline'ının çekirdeği.
    /// </summary>
    public class TopicConfiguration : BaseEntityConfiguration<Topic>
    {
        public override void Configure(EntityTypeBuilder<Topic> builder)
        {
            base.Configure(builder);
            builder.ToTable("Topics");

            // 🔹 Unique Code
            builder.HasIndex(e => e.TopicCode).IsUnique();

            // 🔹 Alan uzunlukları
            builder.Property(e => e.TopicCode)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(e => e.Category)
                .HasMaxLength(64);

            builder.Property(e => e.SubCategory)
                .HasMaxLength(128);

            builder.Property(e => e.Series)
                .HasMaxLength(128);

            builder.Property(e => e.Tone)
                .HasMaxLength(32);

            builder.Property(e => e.PotentialVisual)
                .HasMaxLength(500);

            builder.Property(e => e.RenderStyle)
                .HasMaxLength(64);

            builder.Property(e => e.VoiceHint)
                .HasMaxLength(64);

            builder.Property(e => e.ScriptHint)
                .HasMaxLength(64);

            // 🔹 JSON alanı
            builder.Property(e => e.TopicJson)
                .HasColumnType("nvarchar(max)");

            // 🔹 Bayraklar
            builder.Property(e => e.NeedsFootage)
                .HasDefaultValue(false);

            builder.Property(e => e.FactCheck)
                .HasDefaultValue(false);

            builder.Property(e => e.ScriptGenerated)
                .HasDefaultValue(false);

            // 🔹 İlişkiler
            builder.HasOne(e => e.Prompt)
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Script)
                .WithMany()
                .HasForeignKey(e => e.ScriptId)
                .OnDelete(DeleteBehavior.SetNull);

            // 🔹 Zaman alanları
            builder.Property(e => e.ScriptGeneratedAt)
                .HasColumnType("datetimeoffset");

            builder.Property(e => e.AllowScriptGeneration)
                .HasDefaultValue(true); // ✅ varsayılan olarak açık

            // 🔹 Priority varsayılan değeri
            builder.Property(e => e.Priority)
                .HasDefaultValue(5);
        }
    }
}
