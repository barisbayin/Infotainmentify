using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class JobExecutionConfiguration : BaseEntityConfiguration<JobExecution>, IEntityTypeConfiguration<JobExecution>
    {
        public override void Configure(EntityTypeBuilder<JobExecution> builder)
        {
            // BaseEntity alanlarını uygula (CreatedAt, Removed vs.)
            base.Configure(builder);

            builder.ToTable("JobExecutions");

            // Foreign key
            builder.Property(x => x.JobId)
                   .IsRequired();

            builder.HasOne(x => x.Job)
                   .WithMany()
                   .HasForeignKey(x => x.JobId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Status enum
            builder.Property(x => x.Status)
                   .HasConversion<int>() // Enum -> int
                   .IsRequired();

            // ResultJson
            builder.Property(x => x.ResultJson)
                   .IsRequired()
                   .HasColumnType("nvarchar(max)");

            // ErrorMessage
            builder.Property(x => x.ErrorMessage)
                   .HasMaxLength(1000)
                   .HasColumnType("nvarchar(1000)");

            // Datetime fields
            builder.Property(x => x.StartedAt)
                   .IsRequired()
                   .HasColumnType("datetimeoffset");

            builder.Property(x => x.CompletedAt)
                   .HasColumnType("datetimeoffset");

            // 🔍 Faydalı index’ler
            builder.HasIndex(x => x.JobId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.JobId, x.Status });
        }
    }
}
