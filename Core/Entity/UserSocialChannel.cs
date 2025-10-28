using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class UserSocialChannel : BaseEntity
    {
        // -------------------- İlişkiler --------------------
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser OwnerUser { get; set; } = null!;

        [Required]
        public SocialChannelType ChannelType { get; set; } // YouTube, Instagram, TikTok, etc.

        // -------------------- Kanal Bilgileri --------------------
        [MaxLength(255)]
        public string? ChannelName { get; set; } // "Gemini Türkiye"

        [MaxLength(255)]
        public string? ChannelHandle { get; set; } // "@geminiTR"

        [MaxLength(500)]
        public string? ChannelUrl { get; set; } // "https://youtube.com/@geminiTR"

        [MaxLength(255)]
        public string? PlatformChannelId { get; set; } // Örn: "UCxxxx"

        // -------------------- OAuth / API Token Bilgileri --------------------
        [Column(TypeName = "nvarchar(max)")]
        public string? EncryptedTokensJson { get; set; } // JSON blob: access_token, refresh_token, etc.

        public DateTimeOffset? TokenExpiresAt { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Scopes { get; set; } // Örn: "youtube.readonly, youtube.upload"
    }
}
