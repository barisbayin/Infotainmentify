namespace Application.Models
{
    public class RenderStagePayload
    {
        public int SceneLayoutId { get; set; }

        // Final Videonun Yolu
        public string VideoFilePath { get; set; } = default!;

        // Videonun URL'i (Frontend'de oynatmak için)
        public string VideoUrl { get; set; } = default!;

        public int Width { get; set; }
        public int Height { get; set; }
        public int Fps { get; set; }
        public string AspectRatio { get; set; } = default!;

        public double FileSizeMb { get; set; }
        public double Duration { get; set; }
        public RenderAudioQaReport? AudioQa { get; set; }
    }

    public class RenderAudioQaReport
    {
        public double DurationSec { get; set; }
        public double MeanVolumeDb { get; set; }
        public double MaxVolumeDb { get; set; }
        public double SilenceDurationSec { get; set; }
        public int SilenceSegmentCount { get; set; }
        public double SilenceRatio { get; set; }
        public string Status { get; set; } = "Unknown";
        public List<string> Warnings { get; set; } = new();
    }
}
