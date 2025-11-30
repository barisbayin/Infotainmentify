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

            // Temel Alanlar
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();

            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.Username).HasMaxLength(128).IsRequired();

            b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();

            // DirectoryName kesinlikle boş olamaz (Kayıt anında geçici bir şey de olsa dolmalı)
            b.Property(x => x.DirectoryName).HasMaxLength(256).IsRequired();

            b.Property(x => x.Role).HasConversion<int>();

            // =================================================================
            // İLİŞKİ YAPILANDIRMALARI (RELATIONSHIPS)
            // Kullanıcı silinirse (Hard Delete), altındaki her şey uçsun.
            // =================================================================

            // 1. User -> Concepts
            b.HasMany(u => u.Concepts)
             .WithOne() // Concept tarafında AppUser navigation prop varsa .WithOne(c => c.AppUser)
             .HasForeignKey(c => c.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);

            // 2. User -> AiConnections
            b.HasMany(u => u.AiConnections)
             .WithOne()
             .HasForeignKey(c => c.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);

            // 3. User -> SocialChannels
            b.HasMany(u => u.SocialChannels)
             .WithOne()
             .HasForeignKey(c => c.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);

            // 4. User -> Runs (Video Projeleri)
            b.HasMany(u => u.Runs)
             .WithOne() // Run tarafında .WithOne(r => r.User) varsayıyorum
             .HasForeignKey(r => r.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
