using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class StageConfigConfiguration : BaseEntityConfiguration<StageConfig>, IEntityTypeConfiguration<StageConfig>
    {
        public override void Configure(EntityTypeBuilder<StageConfig> b)
        {
            base.Configure(b);

            b.ToTable("StageConfigs");

            // KRİTİK KURAL:
            // Bir Template içinde aynı sıra numarasına sahip iki stage olamaz.
            // "Hem Topic hem Script 1. sırada çalışsın" diyemeyiz, sıra karışır.
            b.HasIndex(x => new { x.ContentPipelineTemplateId, x.Order }).IsUnique();

            // Enum'ı int olarak sakla (Performans için)
            b.Property(x => x.StageType).HasConversion<int>().IsRequired();

            // PresetId nullable olabilir (Her stage preset gerektirmeyebilir)
            b.Property(x => x.PresetId).IsRequired(false);

            // JSON alanı büyük olabilir
            b.Property(x => x.OptionsJson).IsRequired(false); // Sınır koymuyoruz (nvarchar(max))

            // Template ile ilişki (Zaten Template tarafında da tanımlamıştık, çift dikiş sağlam olur)
            b.HasOne(s => s.ContentPipelineTemplate)
             .WithMany(t => t.StageConfigs)
             .HasForeignKey(s => s.ContentPipelineTemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
