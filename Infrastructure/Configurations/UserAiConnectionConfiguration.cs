using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class UserAiConnectionConfiguration : BaseEntityConfiguration<UserAiConnection>
    {
        public override void Configure(EntityTypeBuilder<UserAiConnection> b)
        {
            b.ToTable("UserAiConnections");
            b.HasIndex(x => new { x.UserId, x.Name }).IsUnique();
            b.Property(x => x.EncryptedCredentialJson).HasColumnType("nvarchar(max)");
        }
    }
}
