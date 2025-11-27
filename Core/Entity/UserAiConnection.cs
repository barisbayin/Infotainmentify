using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class UserAiConnection : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        public AiProviderType Provider { get; set; }

        [MaxLength(200)]
        public string? TextModel { get; set; }

        [MaxLength(200)]
        public string? ImageModel { get; set; }

        [MaxLength(200)]
        public string? VideoModel { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? Temperature { get; set; }

        [Required, MaxLength(4000)]
        public string EncryptedCredentialJson { get; set; } = null!;

        [MaxLength(1000)]
        public string? CredentialFilePath { get; set; }
    }
}
