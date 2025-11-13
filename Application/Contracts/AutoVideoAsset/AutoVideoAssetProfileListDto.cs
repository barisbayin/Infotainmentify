namespace Application.Contracts.AutoVideoAsset
{
    public class AutoVideoAssetProfileListDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = "";
        public string TopicProfileName { get; set; } = "";
        public string ScriptProfileName { get; set; } = "";
        public string? SocialChannelName { get; set; }
        public bool UploadAfterRender { get; set; }
        public bool GenerateThumbnail { get; set; }
        public bool IsActive { get; set; }
    }
}
