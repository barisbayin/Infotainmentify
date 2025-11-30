using System.Text.Json;

namespace Application.Contracts.UserSocialChannel
{
    public class SocialChannelDetailDto
    {
        public int Id { get; set; }
        public string ChannelName { get; set; } = default!;
        public string Platform { get; set; } = default!;
        public string? ChannelHandle { get; set; }
        public string? ChannelUrl { get; set; }
        public string? PlatformChannelId { get; set; }
        public bool IsTokenExpired { get; set; }
        public DateTimeOffset? TokenExpiresAt { get; set; }
        public string? Scopes { get; set; }
    }
}
