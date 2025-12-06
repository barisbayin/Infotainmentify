namespace Application.Models
{
    public class RenderStagePayload
    {
        public int SceneLayoutId { get; set; }

        // Final Videonun Yolu
        public string VideoFilePath { get; set; } = default!;

        // Videonun URL'i (Frontend'de oynatmak için)
        public string VideoUrl { get; set; } = default!;

        public double FileSizeMb { get; set; }
        public double Duration { get; set; }
    }
}
