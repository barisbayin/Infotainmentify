using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ConceptConfiguration : BaseEntityConfiguration<Concept>, IEntityTypeConfiguration<Concept>
    {
        public override void Configure(EntityTypeBuilder<Concept> b)
        {
            base.Configure(b);

            b.ToTable("Concepts");

            // KRİTİK KURAL:
            // Bir kullanıcının aynı isimde iki konsepti olamaz.
            // Ama farklı kullanıcılar "Haberler" diye konsept açabilir.
            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Description).HasMaxLength(300);

            // İlişki: Bir Konsept silinirse, altındaki tüm Template'ler de silinsin.
            // Temizlik imandan gelir. :)
            b.HasMany(c => c.Templates)
             .WithOne(t => t.Concept)
             .HasForeignKey(t => t.ConceptId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
