namespace Application.Contracts.VideoAsset
{
    public class VideoAssetDetailDto : VideoAssetListDto
    {
        public string? MetadataJson { get; set; }
    }
}
