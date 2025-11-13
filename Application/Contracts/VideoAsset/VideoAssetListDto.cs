namespace Application.Contracts.VideoAsset
{
    public class VideoAssetListDto
    {
        public int Id { get; set; }
        public int ScriptId { get; set; }
        public string AssetType { get; set; } = default!;
        public string AssetKey { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public bool IsGenerated { get; set; }
        public bool IsUploaded { get; set; }
        public DateTime? GeneratedAt { get; set; }
        public DateTime? UploadedAt { get; set; }
    }
}
