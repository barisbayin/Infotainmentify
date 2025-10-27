using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class AppUserConfiguration : BaseEntityConfiguration<AppUser>, IEntityTypeConfiguration<AppUser>
    {
        public override void Configure(EntityTypeBuilder<AppUser> b)
        {
            b.ToTable("AppUsers");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();

            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.Username).HasMaxLength(128).IsRequired();

            b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            b.Property(x => x.DirectoryName).HasMaxLength(256).IsRequired();

            b.Property(x => x.Role).HasConversion<int>();
        }
    }
}
