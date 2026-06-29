namespace Application.Models
{
    public class AudioEvent
    {
        public string Type { get; set; } = "voice"; // voice, music, sfx
        public string FilePath { get; set; } = default!;
        public int SceneNumber { get; set; }
        public double StartTime { get; set; }
        public double Duration { get; set; }
        public double SourceStartOffsetSec { get; set; }
        public double Volume { get; set; } = 1.0;
        public double FadeInSec { get; set; }
        public double FadeOutSec { get; set; }
        public string EditTransition { get; set; } = "";
        public double EditOffsetSec { get; set; }
        public bool Loop { get; set; } = false;
    }
}
