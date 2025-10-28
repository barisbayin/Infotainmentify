using Core.Enums;

namespace Application.Contracts.UserSocialChannel
{
    public class UserSocialChannelListDto
    {
        public int Id { get; set; }
        public SocialChannelType ChannelType { get; set; }
        public string? ChannelName { get; set; }
        public string? ChannelHandle { get; set; }
        public string? ChannelUrl { get; set; }
        public DateTimeOffset LastVerifiedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
