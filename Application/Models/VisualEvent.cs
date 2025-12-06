namespace Application.Models
{
    public class VisualEvent
    {
        public int SceneIndex { get; set; }
        public string ImagePath { get; set; } = default!;
        public double StartTime { get; set; }
        public double Duration { get; set; }

        // Efektler
        public string EffectType { get; set; } = "zoom_in"; // zoom_in, zoom_out, pan_left...
        public double ZoomIntensity { get; set; } = 1.1;    // %10 büyüme
        public string TransitionType { get; set; } = "fade";
        public double TransitionDuration { get; set; } = 0.5;
    }
}
