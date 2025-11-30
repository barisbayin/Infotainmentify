namespace Application.Contracts.UserSocialChannel
{
    public class SocialChannelListDto
    {
        public int Id { get; set; }
        public string ChannelName { get; set; } = default!; // "Gemini TR"
        public string Platform { get; set; } = default!;    // "YouTube"
        public string? ChannelUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
