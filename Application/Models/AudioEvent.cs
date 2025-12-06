namespace Application.Models
{
    public class AudioEvent
    {
        public string Type { get; set; } = "voice"; // voice, music, sfx
        public string FilePath { get; set; } = default!;
        public double StartTime { get; set; }
        public double Volume { get; set; } = 1.0;
        public bool Loop { get; set; } = false;
    }
}
