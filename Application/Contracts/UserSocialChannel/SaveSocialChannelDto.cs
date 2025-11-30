using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.UserSocialChannel
{
    public class SaveSocialChannelDto
    {
        [Required]
        public SocialChannelType ChannelType { get; set; }

        [Required, MaxLength(255)]
        public string ChannelName { get; set; } = default!;

        [MaxLength(255)]
        public string? ChannelHandle { get; set; }

        [MaxLength(500)]
        public string? ChannelUrl { get; set; }

        [MaxLength(255)]
        public string? PlatformChannelId { get; set; }

        // Tokenlar JSON string olarak gelir (Frontend OAuth flow'dan alır)
        public string? RawTokensJson { get; set; }

        public DateTimeOffset? TokenExpiresAt { get; set; }

        public string? Scopes { get; set; }
    }
}
