using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class ScriptConfiguration : BaseEntityConfiguration<Script>, IEntityTypeConfiguration<Script>
    {
        public override void Configure(EntityTypeBuilder<Script> b)
        {
            base.Configure(b);
            b.ToTable("Scripts");

            b.HasIndex(x => x.AppUserId);
            b.HasIndex(x => x.TopicId);

            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Content).IsRequired(); // NVARCHAR(MAX)
            b.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();

            // JSON alanı
            b.Property(x => x.ScenesJson).IsRequired(false);

            // İlişkiler
            b.HasOne(x => x.AppUser)
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);

            // Topic silinirse Script de silinsin mi? 
            // Genelde EVET. Fikir giderse senaryo yetim kalır.
            b.HasOne(x => x.Topic)
             .WithMany() // Topic altında "Scripts" listesi tutmadık, gerekirse ekleriz.
             .HasForeignKey(x => x.TopicId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
