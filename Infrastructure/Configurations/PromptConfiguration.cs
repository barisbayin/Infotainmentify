using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class PromptConfiguration : BaseEntityConfiguration<Prompt>, IEntityTypeConfiguration<Prompt>
    {
        public override void Configure(EntityTypeBuilder<Prompt> builder)
        {
            // BaseEntity alanlarını ve soft-delete filtresini uygula
            base.Configure(builder);

            builder.ToTable("Prompts");

            builder.Property(p => p.AppUserId)
                    .IsRequired()
                    .HasColumnType("int");

            // Kolon kuralları (SQL Server)
            builder.Property(p => p.Name)
                   .IsRequired()
                   .HasMaxLength(200)
                   .HasColumnType("nvarchar(200)");

            builder.Property(p => p.Category)
                   .HasMaxLength(64)
                   .HasColumnType("nvarchar(64)");

            builder.Property(p => p.Language)
                   .HasMaxLength(10)
                   .HasColumnType("nvarchar(10)");

            builder.Property(p => p.IsActive)
                   .HasColumnType("bit")
                   .HasDefaultValue(true);

            builder.Property(p => p.Body)
                   .IsRequired()
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.Description)
                    .HasMaxLength(1000);

            builder.Property(p => p.SystemPrompt)
                   .HasColumnType("nvarchar(max)");


            // Faydalı indeksler
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => p.Category);
            builder.HasIndex(p => p.IsActive);
        }
    }
}
