using System.Text.Json;

namespace Application.Contracts.UserSocialChannel
{
    public class UserSocialChannelDetailDto : UserSocialChannelListDto
    {
        public string? PlatformChannelId { get; set; }
        public Dictionary<string, object>? Tokens { get; set; }
        public DateTimeOffset? TokenExpiresAt { get; set; }
        public string? Scopes { get; set; }
    }
}
