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
        public string OverlayText { get; set; } = "";
        public string Emphasis { get; set; } = "";

        // B-roll / visual beat metadata. Render motoru icin opsiyonel, debug ve UI icin faydali.
        public string VisualRole { get; set; } = "primary"; // primary, broll_auto, broll_prompt
        public string VisualType { get; set; } = "";
        public string VarietyRole { get; set; } = "";
        public string VarietyReason { get; set; } = "";
        public string SegmentRole { get; set; } = "";
        public int SegmentIndex { get; set; } = 1;
        public int SegmentCount { get; set; } = 1;
        public string ShotType { get; set; } = "";
        public string DirectorIntent { get; set; } = "";
        public string CutReason { get; set; } = "";
        public string AudioTransition { get; set; } = "";
        public double AudioOffsetSec { get; set; }
        public string ChapterTitle { get; set; } = "";
        public string CaptionMode { get; set; } = "";
        public string MusicEnergy { get; set; } = "";
        public string ContinuityAnchor { get; set; } = "";
        public string Composition { get; set; } = "";
        public int VisualQualityScore { get; set; }
        public string VisualQualityNotes { get; set; } = "";
        public int SourceImageSceneNumber { get; set; }
        public int SourceImageBeatIndex { get; set; }
        public bool IsFallbackImage { get; set; }
    }
}
