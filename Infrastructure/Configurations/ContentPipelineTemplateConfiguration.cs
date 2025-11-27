using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ContentPipelineTemplateConfiguration : BaseEntityConfiguration<ContentPipelineTemplate>
    {
        public override void Configure(EntityTypeBuilder<ContentPipelineTemplate> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.HasOne(x => x.Concept)
                .WithMany()                   // Concept -> N PipelineTemplate
                .HasForeignKey(x => x.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StageConfigs)
                .WithOne(x => x.ContentPipelineTemplate)     // StageConfig.Pipeline = PipelineTemplate
                .HasForeignKey(x => x.ContentPipelineTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
