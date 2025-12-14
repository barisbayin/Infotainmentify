using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ContentPipelineRunConfiguration : BaseEntityConfiguration<ContentPipelineRun>, IEntityTypeConfiguration<ContentPipelineRun>
    {
        public override void Configure(EntityTypeBuilder<ContentPipelineRun> b)
        {
            base.Configure(b);

            b.ToTable("ContentPipelineRuns");

            // Hız için Status ve User bazlı indexler
            // "Barış'ın başarısız olan videolarını getir" sorgusu için:
            b.HasIndex(x => new { x.AppUserId, x.Status });

            // Enum dönüşümü
            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            // Hata mesajı uzun olabilir ama sınır koyalım (NVARCHAR(MAX) olmasın, indexlenemez)
            b.Property(x => x.ErrorMessage).HasMaxLength(4000);

            b.Property(x => x.Language)
            .HasMaxLength(10) // "en-US", "tr-TR", "zh-CN" için 10 fazlasıyla yeter
            .HasDefaultValue("en-US") // Mevcut kayıtlar null kalmasın diye
            .IsRequired();

            // İLİŞKİLER

            // 1. Run -> StageExecutions
            // Run silinirse altındaki tüm adım logları silinsin.
            b.HasMany(r => r.StageExecutions)
             .WithOne(s => s.Run) // StageExecution tarafında "Run" adında prop olmalı
             .HasForeignKey(s => s.ContentPipelineRunId)
             .OnDelete(DeleteBehavior.Restrict);

            // 2. User ve Template ilişkileri
            // Bunları zaten AppUserConfiguration ve TemplateConfiguration tarafında tanımlamıştık.
            // Ama burada da Explicit (Açık) olarak belirtmek her zaman daha güvenlidir.
            b.HasOne(r => r.AppUser)
             .WithMany(u => u.Runs)
             .HasForeignKey(r => r.AppUserId)
             .OnDelete(DeleteBehavior.NoAction); // DİKKAT: User silinirse zaten UserConfig'deki Cascade çalışır. Burayı NoAction bırakmak döngüsel hatayı önler.
        }
    }
}
