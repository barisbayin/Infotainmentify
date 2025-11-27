namespace Application.Contracts.AutoVideoAsset
{
    public class VideoGenerationProfileListDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = null!;
        public bool IsActive { get; set; }

        // Script Profile info
        public int ScriptGenerationProfileId { get; set; }
        public string ScriptGenerationProfileName { get; set; } = null!;

        public int AutoVideoRenderProfileId { get; set; }
        public string AutoVideoRenderProfileName { get; set; } = null!;

        // Upload Channel
        public int? SocialChannelId { get; set; }
        public string? SocialChannelName { get; set; }
    }
}
