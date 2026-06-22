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
    }
}
