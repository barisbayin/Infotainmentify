using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ConceptConfiguration : BaseEntityConfiguration<Concept>
    {
        public override void Configure(EntityTypeBuilder<Concept> builder)
        {
            base.Configure(builder);

            builder.ToTable("Concepts");

            builder.Property(x => x.AppUserId)
                .IsRequired();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(300);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);
        }
    }
}
