using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class StageConfigConfiguration : BaseEntityConfiguration<StageConfig>
    {
        public override void Configure(EntityTypeBuilder<StageConfig> builder)
        {
            base.Configure(builder);

            builder.ToTable("StageConfigs");

            builder.Property(x => x.StageType)
                .IsRequired();

            builder.HasOne(x => x.ContentPipelineTemplate)
                .WithMany(p => p.StageConfigs)
                .HasForeignKey(x => x.ContentPipelineTemplate)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
