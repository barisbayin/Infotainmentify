namespace Application.Models
{
    public class SubtitleItem
    {
        public double t { get; set; }   // Start time (seconds)
        public double d { get; set; }   // Duration (seconds)
        public string text { get; set; } = "";
    }
}
