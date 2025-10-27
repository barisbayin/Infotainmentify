using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class UserSocialChannel : BaseEntity
    {
        // Hangi platform? (YouTube, Instagram vs.)
        [Required]
        public SocialChannelType ChannelType { get; set; }

        // Hangi kullanıcıya ait? (AppUser'a Foreign Key)
        [Required]
        public int AppUserId { get; set; } // Veya tipiniz int/Guid ise o olmalı

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser AppUser { get; set; }

        // --- Kanal Bilgileri ---

        [Required]
        [StringLength(255)]
        public string ChannelName { get; set; } // Örn: "Gemini Türkiye"

        [StringLength(255)]
        public string ChannelHandle { get; set; } // Örn: "@geminiTR" (varsa)

        [StringLength(500)]
        public string ChannelUrl { get; set; } // Örn: "https://youtube.com/c/geminiTR"

        [StringLength(255)]
        public string PlatformChannelId { get; set; } // Platformun verdiği ID (Örn: "UC...")

        // --- API Bilgileri (ÇOK ÖNEMLİ: BU ALANLAR VERİTABANINDA ŞİFRELENMELİ!) ---

        [Column(TypeName = "nvarchar(max)")]
        public string EncryptedTokensJson { get; set; }

        public DateTime? TokenExpiresAt { get; set; } // AccessToken'ın ne zaman geçersiz olacağı

        [StringLength(1000)]
        public string Scopes { get; set; } // Kullanıcının hangi izinleri verdiği (örn: "read:profile write:video")

        // --- Diğer Bilgiler ---
        public DateTime LastVerifiedAt { get; set; } = DateTime.Now;
    }
}
