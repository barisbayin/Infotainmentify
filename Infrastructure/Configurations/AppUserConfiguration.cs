using Core.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class AppUserConfiguration : BaseEntityConfiguration<AppUser>, IEntityTypeConfiguration<AppUser>
    {
        public override void Configure(EntityTypeBuilder<AppUser> b)
        {
            // Base yapılandırmayı çağır (Id, CreatedAt vs.)
            base.Configure(b);

            b.ToTable("AppUsers");

            // --------------------------------------------------------
            // TEMEL ALANLAR (Sadece kendi propertyleri kalsın)
            // --------------------------------------------------------
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();

            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.Username).HasMaxLength(128).IsRequired();

            b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();

            b.Property(x => x.DirectoryName).HasMaxLength(256).IsRequired();

            b.Property(x => x.Role).HasConversion<int>();

            // --------------------------------------------------------
            // ❌ İLİŞKİLER SİLİNDİ
            // --------------------------------------------------------
            // Buradaki tüm HasMany...WithOne bloklarını sildik.
            // Çünkü bu ilişkiler ConceptConfiguration, UserAiConnectionConfiguration
            // gibi karşı tarafın dosyalarında zaten "HasOne(...).WithMany(...)" 
            // şeklinde tanımlı. Çift dikiş atmaya gerek yok, hataya sebep oluyor.
        }
    }
}
