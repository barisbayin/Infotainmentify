using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class TopicConfiguration : IEntityTypeConfiguration<Topic>
    {
        public void Configure(EntityTypeBuilder<Topic> builder)
        {
            builder.ToTable("Topics");

            builder.HasKey(t => t.Id);

            builder.Property(p => p.UserId)
                   .IsRequired()
                   .HasColumnType("int");

            builder.Property(t => t.TopicCode)
                   .IsRequired()
                   .HasMaxLength(64)
                   .HasColumnType("nvarchar(64)");

            builder.Property(t => t.Category)
                   .HasMaxLength(64)
                   .HasColumnType("nvarchar(64)");

            builder.Property(t => t.PremiseTr)
                   .HasColumnType("nvarchar(max)");

            builder.Property(t => t.Premise)
                   .HasColumnType("nvarchar(max)");

            builder.Property(t => t.Tone)
                   .HasMaxLength(32)
                   .HasColumnType("nvarchar(32)");

            builder.Property(t => t.PotentialVisual)
                   .HasColumnType("nvarchar(max)");

            builder.Property(t => t.NeedsFootage)
                   .HasColumnType("bit")
                   .HasDefaultValue(false);

            builder.Property(t => t.FactCheck)
                   .HasColumnType("bit")
                   .HasDefaultValue(false);

            builder.Property(t => t.TagsJson)
                   .HasColumnType("nvarchar(max)");

            builder.Property(t => t.TopicJson)
                   .HasColumnType("nvarchar(max)");

            // İlişki: Topic → Prompt (opsiyonel), Prompt silinirse NULL’a çek
            builder.HasOne(t => t.Prompt)
                   .WithMany()
                   .HasForeignKey(t => t.PromptId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Indexler
            builder.HasIndex(x => new { x.UserId, x.TopicCode }).IsUnique();
            builder.HasIndex(t => t.TopicCode).IsUnique();
            builder.HasIndex(t => t.Category);
            builder.HasIndex(t => t.PromptId);
        }
    }
}
