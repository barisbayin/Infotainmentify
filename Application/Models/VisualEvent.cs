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

        // B-roll / visual beat metadata. Render motoru icin opsiyonel, debug ve UI icin faydali.
        public string VisualRole { get; set; } = "primary"; // primary, broll_auto, broll_prompt
        public int SegmentIndex { get; set; } = 1;
        public int SegmentCount { get; set; } = 1;
    }
}
