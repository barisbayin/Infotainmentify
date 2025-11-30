using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class JobSettingConfiguration : BaseEntityConfiguration<JobSetting>, IEntityTypeConfiguration<JobSetting>
    {
        public override void Configure(EntityTypeBuilder<JobSetting> builder)
        {
            // BaseEntity alanlarını uygula (CreatedAt, Removed vs.)
            base.Configure(builder);

            builder.ToTable("JobSettings");

            // Zorunlu alanlar
            builder.Property(x => x.JobType)
                   .HasConversion<int>() // Enum -> int
                   .IsRequired();

            builder.Property(x => x.Name)
                   .HasMaxLength(200)
                   .IsRequired()
                   .HasColumnType("nvarchar(200)");

            builder.Property(x => x.AppUserId)
                   .IsRequired();

            // İlişki
            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.AppUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ProfileId)
                   .IsRequired();

            builder.Property(x => x.ProfileType)
                   .HasMaxLength(300)
                   .IsRequired()
                   .HasColumnType("nvarchar(300)");

            // Schedule alanları
            builder.Property(x => x.IsAutoRunEnabled)
                   .HasColumnType("bit")
                   .HasDefaultValue(false);

            builder.Property(x => x.PeriodHours)
                   .HasColumnType("decimal(5,2)")
                   .HasDefaultValue(null);

            builder.Property(x => x.LastRunAt)
                   .HasColumnType("datetimeoffset");

            // Status & Error
            builder.Property(x => x.Status)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(x => x.LastError)
                   .HasMaxLength(1000)
                   .HasColumnType("nvarchar(1000)");

            // 🔍 Faydalı index’ler
            builder.HasIndex(x => x.JobType);
            builder.HasIndex(x => x.AppUserId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.JobType, x.Status });
        }
    }
}
