namespace Application.Models
{
    public class ThumbnailStagePayload
    {
        public int ScriptId { get; set; }
        public string ThumbnailFilePath { get; set; } = default!;
        public string ThumbnailUrl { get; set; } = default!;
        public string PromptUsed { get; set; } = default!;
        public int Width { get; set; }
        public int Height { get; set; }
        public YouTubePackagePayload YouTubePackage { get; set; } = new();
    }

    public class YouTubePackagePayload
    {
        public List<string> TitleOptions { get; set; } = new();
        public List<ThumbnailConceptItem> ThumbnailConcepts { get; set; } = new();
        public string Description { get; set; } = "";
        public List<YouTubeChapterItem> Chapters { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<string> Hashtags { get; set; } = new();
        public string PinnedComment { get; set; } = "";
        public List<string> UploadChecklist { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class ThumbnailConceptItem
    {
        public string Name { get; set; } = "";
        public string Prompt { get; set; } = "";
        public string Rationale { get; set; } = "";
    }

    public class YouTubeChapterItem
    {
        public string Timestamp { get; set; } = "";
        public string Title { get; set; } = "";
        public double StartSec { get; set; }
    }
}
