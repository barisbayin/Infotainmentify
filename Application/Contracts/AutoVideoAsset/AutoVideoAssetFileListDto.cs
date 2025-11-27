namespace Application.Contracts.AutoVideoAsset
{
    public class AutoVideoAssetFileListDto
    {
        public int Id { get; set; }
        public int SceneNumber { get; set; }
        public string FilePath { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public bool IsGenerated { get; set; }
    }
}
