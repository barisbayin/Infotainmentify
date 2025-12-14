using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AssetFileConfiguration : BaseEntityConfiguration<AssetFile>
    {
        public override void Configure(EntityTypeBuilder<AssetFile> builder)
        {
            // 1. Önce Base ayarlarını (Id, SoftDelete, CreatedAt vb.) uygula
            base.Configure(builder);

            // 2. Tablo Adı
            builder.ToTable("AssetFiles");

            // 3. Property Ayarları
            builder.Property(x => x.FriendlyName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.PhysicalFileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.ContentType)
                .HasMaxLength(100)
                .HasDefaultValue("application/octet-stream");

            builder.Property(x => x.SizeInBytes)
                .IsRequired();

            // DurationSec nullable olduğu için ekstra ayara gerek yok ama
            // istersen default değer verebilirsin (genelde gerekmez)
            // builder.Property(x => x.DurationSec); 

            // 4. İlişkiler
            builder.HasOne(x => x.AppUser)
                .WithMany() // Eğer AppUser içinde "ICollection<AssetFile>" yoksa boş bırakılır
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade); // User silinirse dosyaları da gitsin

            // 5. İndeksler (Performans)
            // Serviste "GetUserAssetsAsync(userId, type)" çağırdığımız için
            // bu ikiliye Composite Index atmak sorguyu çok hızlandırır.
            builder.HasIndex(x => new { x.AppUserId, x.Type });
        }
    }
}
