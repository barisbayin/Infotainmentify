namespace Application.Models
{
    public class SocialMetadata
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new();
        public string PrivacyStatus { get; set; }
        public string? ThumbnailPath { get; set; }
    }
}
