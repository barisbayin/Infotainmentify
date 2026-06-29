namespace Application.Models
{

    public class SceneImageItem
    {
        public int SceneNumber { get; set; }
        public int BeatIndex { get; set; } = 1;
        public int BeatCount { get; set; } = 1;
        public string BeatRole { get; set; } = "primary";
        public string VisualType { get; set; } = "";
        public string VarietyRole { get; set; } = "";
        public string VarietyReason { get; set; } = "";
        public string ShotType { get; set; } = "";
        public string EffectType { get; set; } = "zoom_in";
        public string TransitionType { get; set; } = "cut";
        public string OverlayText { get; set; } = "";
        public string DirectorIntent { get; set; } = "";
        public string ContinuityAnchor { get; set; } = "";
        public string Composition { get; set; } = "";
        public string Lens { get; set; } = "";
        public string Lighting { get; set; } = "";
        public string ColorNotes { get; set; } = "";
        public string CutIntent { get; set; } = "";
        public int VisualQualityScore { get; set; }
        public string VisualQualityNotes { get; set; } = "";
        public string ImagePath { get; set; } = default!; // "/users/1/runs/10/scene_1.png"
        public string PromptUsed { get; set; } = default!; // Debug için
    }
}
