using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class UserAiConnection : BaseEntity // Id, Created/Updated, CreatedById ...
    {
        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!; // "My OpenAI (work)"

        [Required]
        public AiProviderType Provider { get; set; }

        [Required]
        public AiAuthType AuthType { get; set; }

        [Required]
        public AiCapability Capabilities { get; set; }

        public bool IsDefaultForText { get; set; }

        public bool IsDefaultForImage { get; set; }

        public bool IsDefaultForEmbedding { get; set; }

        // Encrypted blob (JSON) - never plain text
        [Required] public string EncryptedCredentialJson { get; set; } = null!;

        public DateTimeOffset? AccessTokenExpiresAt { get; set; } // OAuth
    }
}
