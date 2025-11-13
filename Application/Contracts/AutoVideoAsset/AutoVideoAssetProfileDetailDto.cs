namespace Application.Contracts.AutoVideoAsset
{
    public class AutoVideoAssetProfileDetailDto
    {
        public int Id { get; set; }

        public string ProfileName { get; set; } = "";

        public int TopicGenerationProfileId { get; set; }
        public int ScriptGenerationProfileId { get; set; }
        public int? SocialChannelId { get; set; }

        public bool UploadAfterRender { get; set; } = true;
        public bool GenerateThumbnail { get; set; } = true;

        public string? TitleTemplate { get; set; }
        public string? DescriptionTemplate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
