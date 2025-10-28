using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class UserSocialChannelConfiguration : BaseEntityConfiguration<UserSocialChannel>
    {
        public override void Configure(EntityTypeBuilder<UserSocialChannel> b)
        {
            b.ToTable("UserSocialChannels");

            // --- Benzersiz Kural (Index) ---
            // Bir kullanıcının her platform (ChannelType) için yalnızca bir kaydı olabilir.
            b.HasIndex(x => new { x.AppUserId, x.ChannelType }).IsUnique();

            // --- İlişkiler (Relationships) ---
            // Her UserSocialChannel bir AppUser'a aittir.
            // AppUser silinirse, ona bağlı kanallar da silinsin (Cascade).
            //b.HasOne(sc => sc.AppUser)
            // .WithMany(u => u.SocialChannels) // AppUser sınıfındaki ICollection<UserSocialChannel>
            // .HasForeignKey(sc => sc.AppUserId)
            // .OnDelete(DeleteBehavior.Cascade);

            // --- Sütun Ayarları (Properties) ---

            // Enum'ı veritabanında integer olarak tut
            b.Property(x => x.ChannelType).HasConversion<int>().IsRequired();

            b.Property(x => x.ChannelName).HasMaxLength(255).IsRequired();

            // Bu alanlar nullable (boş geçilebilir) olabilir
            b.Property(x => x.ChannelHandle).HasMaxLength(255);
            b.Property(x => x.ChannelUrl).HasMaxLength(500);
            b.Property(x => x.PlatformChannelId).HasMaxLength(255);
            b.Property(x => x.Scopes).HasMaxLength(1000);

            // Token alanları potansiyel olarak çok uzun olabilir ve şifrelenince uzar.
            // Bu yüzden HasMaxLength yerine HasColumnType kullanmak daha sağlıklıdır.

        }
    }
}
