using Core.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ScriptConfiguration : IEntityTypeConfiguration<Script>
    {
        public void Configure(EntityTypeBuilder<Script> b)
        {
            b.ToTable("Scripts");

            b.HasKey(x => x.Id);

            b.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            b.Property(x => x.Content)
                .IsRequired();

            b.Property(x => x.Summary)
                .HasMaxLength(200);

            b.Property(x => x.Language)
                .HasMaxLength(50);

            b.Property(x => x.MetaJson)
                .HasColumnType("nvarchar(max)");

            b.HasOne(x => x.Topic)
                .WithMany()
                .HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.NoAction);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
