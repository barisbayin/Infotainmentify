using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.User
{
    public class UserSocialChannel : BaseEntity
    {
        // -------------------- İlişkiler --------------------
        [Required]
        public int AppUserId { get; set; }

        // Diğer sınıflarda hep "AppUser" kullandık, standart bozulmasın.
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public SocialChannelType ChannelType { get; set; } // YouTube, Instagram...

        // -------------------- Kanal Bilgileri --------------------
        [MaxLength(255)]
        public string? ChannelName { get; set; } // "Gemini Türkiye"

        [MaxLength(255)]
        public string? ChannelHandle { get; set; } // "@geminiTR"

        [MaxLength(500)]
        public string? ChannelUrl { get; set; }

        // Platformun kendi ID'si (YouTube Channel ID, Instagram Account ID)
        // Aynı kanalı tekrar eklemeyi engellemek için kritik.
        [MaxLength(255)]
        public string? PlatformChannelId { get; set; }

        // -------------------- OAuth / API Token Bilgileri --------------------

        // Burası sistemin anahtarı. AES ile şifreli JSON string.
        // İçerik: { "access_token": "...", "refresh_token": "...", "client_id": "..." }
        public string? EncryptedTokensJson { get; set; }

        // Token ne zaman ölecek? (Refresh Token logic için şart)
        public DateTimeOffset? TokenExpiresAt { get; set; }

        // Hangi yetkileri aldık?
        public string? Scopes { get; set; }
    }
}
