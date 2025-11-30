using Core.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class UserSocialChannelConfiguration : BaseEntityConfiguration<UserSocialChannel>, IEntityTypeConfiguration<UserSocialChannel>
    {
        public override void Configure(EntityTypeBuilder<UserSocialChannel> b)
        {
            base.Configure(b);

            b.ToTable("UserSocialChannels");

            // KURAL: Bir kullanıcı aynı YouTube kanalını iki kere eklemesin.
            // (AppUserId + PlatformChannelId) Unique olmalı.
            // Amaç: Kullanıcı yanlışlıkla "Gemini TR" kanalını iki kere bağlamasın.
            b.HasIndex(x => new { x.AppUserId, x.PlatformChannelId }).IsUnique();

            b.Property(x => x.ChannelType).HasConversion<int>().IsRequired();

            b.Property(x => x.ChannelName).HasMaxLength(255);
            b.Property(x => x.ChannelHandle).HasMaxLength(255);
            b.Property(x => x.PlatformChannelId).HasMaxLength(255);

            // Token ve Scope alanları uzun olabilir (NVARCHAR(MAX) veya TEXT)
            b.Property(x => x.EncryptedTokensJson).IsRequired(false);
            b.Property(x => x.Scopes).IsRequired(false);

            // İlişki (Cascade Delete)
            // AppUserConfig tarafında tanımlamıştık ama burada da belirtelim.
            b.HasOne(x => x.AppUser)
             .WithMany(u => u.SocialChannels)
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
