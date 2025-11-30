using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class StageExecutionConfiguration : BaseEntityConfiguration<StageExecution>, IEntityTypeConfiguration<StageExecution>
    {
        public override void Configure(EntityTypeBuilder<StageExecution> b)
        {
            base.Configure(b);

            b.ToTable("StageExecutions");

            // Sorgu Hızı İçin:
            // "Bu Run'ın başarısız olan stage'lerini getir"
            b.HasIndex(x => new { x.ContentPipelineRunId, x.Status });

            // "Hangi Config daha çok hata veriyor?" analizi için
            b.HasIndex(x => new { x.StageConfigId, x.Status });

            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            // JSON ALANLARI (Büyük Veri)
            // SQL Server için NVARCHAR(MAX), Postgres için text/jsonb
            b.Property(x => x.InputJson).IsRequired(false);
            b.Property(x => x.OutputJson).IsRequired(false);
            b.Property(x => x.LogsJson).IsRequired(false);
            b.Property(x => x.Error).IsRequired(false);

            // İLİŞKİLER

            // 1. Run -> StageExecution
            b.HasOne(se => se.Run)
             .WithMany(r => r.StageExecutions)
             .HasForeignKey(se => se.ContentPipelineRunId)
             .OnDelete(DeleteBehavior.Restrict); // Run silinirse loglar da silinsin.

            // 2. StageConfig -> StageExecution
            // Config silinirse loglar silinsin mi? 
            // Normalde "Hayır, tarihçe kalsın" denir ama Config silindiyse o logun anlamı kalmayabilir.
            // Cascade mantıklı.
            b.HasOne(se => se.StageConfig)
             .WithMany() // Config tarafında "Executions" listesi tutmadık (gerek yok)
             .HasForeignKey(se => se.StageConfigId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
