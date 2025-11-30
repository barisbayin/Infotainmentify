using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    /// <summary>
    /// Topic entity configuration
    /// İçerik üretim pipeline'ının çekirdeği.
    /// </summary>
    public sealed class TopicConfiguration : BaseEntityConfiguration<Topic>, IEntityTypeConfiguration<Topic>
    {
        public override void Configure(EntityTypeBuilder<Topic> b)
        {
            base.Configure(b);
            b.ToTable("Topics");

            // Kullanıcı bazlı arama hızı için
            b.HasIndex(x => x.AppUserId);
            b.HasIndex(x => x.Category);

            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Premise).IsRequired(); // NVARCHAR(MAX)
            b.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();

            // JSON alanları
            b.Property(x => x.TagsJson).IsRequired(false);
            b.Property(x => x.RawJsonData).IsRequired(false);

            // İlişkiler
            b.HasOne(x => x.AppUser)
             .WithMany() // User tarafında listeye gerek yoksa boş bırak
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
