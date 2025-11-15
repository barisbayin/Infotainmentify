namespace Application.Contracts.AutoVideoAsset
{
    public class VideoGenerationProfileDetailDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = null!;

        public int ScriptGenerationProfileId { get; set; }

        public int? SocialChannelId { get; set; }

        public bool UploadAfterRender { get; set; }
        public bool GenerateThumbnail { get; set; }

        public string? TitleTemplate { get; set; }
        public string? DescriptionTemplate { get; set; }

        public bool IsActive { get; set; }
    }
}
