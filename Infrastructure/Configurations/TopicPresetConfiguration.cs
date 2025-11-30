using Core.Entity.Presets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public sealed class TopicPresetConfiguration : BaseEntityConfiguration<TopicPreset>, IEntityTypeConfiguration<TopicPreset>
    {
        public override void Configure(EntityTypeBuilder<TopicPreset> b)
        {
            base.Configure(b);

            b.ToTable("TopicPresets");

            // Kullanıcı aynı isimde iki preset oluşturmasın
            b.HasIndex(x => new { x.AppUserId, x.Name }).IsUnique();

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            b.Property(x => x.PromptTemplate).HasMaxLength(5000).IsRequired(); // NVARCHAR(MAX) olmasın, 5000 iyidir.

            b.Property(x => x.ContextKeywordsJson).IsRequired(false);

            // İLİŞKİLER

            // 1. User -> TopicPresets
            b.HasOne(x => x.AppUser)
             .WithMany() // User tarafında "TopicPresets" listesi tutmadık (Gerekirse ekleriz)
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // 2. AiConnection -> TopicPresets
            // Eğer AI Connection silinirse, bu preset kullanılamaz hale gelir.
            // Restrict yapıyoruz ki, kullanılan bir connection silinemesin.
            b.HasOne(x => x.UserAiConnection)
             .WithMany()
             .HasForeignKey(x => x.UserAiConnectionId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
