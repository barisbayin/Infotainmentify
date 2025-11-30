using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ContentPipelineTemplateConfiguration : BaseEntityConfiguration<ContentPipelineTemplate>, IEntityTypeConfiguration<ContentPipelineTemplate>
    {
        public override void Configure(EntityTypeBuilder<ContentPipelineTemplate> b)
        {
            base.Configure(b); // Id, CreatedAt vs.

            b.ToTable("ContentPipelineTemplates");

            // İsim Çakışması Kuralı:
            // Aynı Konsept içinde aynı isimde iki şablon olmasın.
            // "Korku Kanalı" içinde iki tane "Shorts V1" olmasın, kafa karışır.
            b.HasIndex(x => new { x.ConceptId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);

            // İLİŞKİLER

            // 1. Template -> StageConfigs (Tarifin adımları)
            // Template silinirse adımları da silinsin.
            b.HasMany(t => t.StageConfigs)
             .WithOne(s => s.ContentPipelineTemplate)
             .HasForeignKey(s => s.ContentPipelineTemplateId)
             .OnDelete(DeleteBehavior.Cascade);

            // 2. Template -> Runs (Üretim Geçmişi)
            // Template silinirse bu şablonla üretilen run kayıtları da silinsin.
            b.HasMany(t => t.Runs)
             .WithOne(r => r.Template)
             .HasForeignKey(r => r.TemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
