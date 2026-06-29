namespace Application.Models
{
    public class RenderProgressUpdate
    {
        public int RunId { get; set; }
        public string Stage { get; set; } = "Render";
        public string Label { get; set; } = "Final render";
        public double Percent { get; set; }
        public double CurrentSeconds { get; set; }
        public double TotalSeconds { get; set; }
        public int? ChunkIndex { get; set; }
        public int? TotalChunks { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
