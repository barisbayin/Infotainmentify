using Core.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class UserAiConnectionConfiguration : BaseEntityConfiguration<UserAiConnection>, IEntityTypeConfiguration<UserAiConnection>
    {
        public override void Configure(EntityTypeBuilder<UserAiConnection> b)
        {
            base.Configure(b);

            b.ToTable("UserAiConnections");

            // Kullanıcı aynı isimde iki bağlantı oluşturmasın (Opsiyonel ama iyi UX)
            // Örn: İki tane "OpenAI Hesabım" olmasın.
            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();

            // API Key uzun olabilir (Google Service Account JSON ~2-3KB)
            b.Property(x => x.EncryptedApiKey).HasMaxLength(4000).IsRequired();

            b.Property(x => x.Provider).HasConversion<int>(); // Enum int olarak saklansın
        }
    }
}
