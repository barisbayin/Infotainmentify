namespace Application.Contracts.AutoVideoAsset
{
    public class AutoVideoAssetFileDetailDto
    {
        public int Id { get; set; }
        public int SceneNumber { get; set; }
        public string FilePath { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public string? AssetKey { get; set; }
        public bool IsGenerated { get; set; }

        // Thumbnail, duration gibi şeyler için generic Metadata
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
