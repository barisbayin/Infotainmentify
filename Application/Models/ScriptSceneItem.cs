namespace Application.Models
{
    public class ScriptSceneItem
    {
        public int SceneNumber { get; set; }
        public string VisualPrompt { get; set; } = default!; // Resim AI için tarif
        public string AudioText { get; set; } = default!;    // TTS için konuşma metni
        public int EstimatedDuration { get; set; } // Saniye
        public string SceneRole { get; set; } = "";
        public string ScenePurpose { get; set; } = "";
        public string ViewerQuestion { get; set; } = "";
        public string EmotionalBeat { get; set; } = "";
        public string VisualType { get; set; } = "";
        public string VisualVarietyRole { get; set; } = "";
        public string VisualVarietyReason { get; set; } = "";
        public string CameraPlan { get; set; } = "";
        public string OverlayText { get; set; } = "";
        public string SfxCue { get; set; } = "";
        public string TransitionIntent { get; set; } = "";
        public string ChapterTitle { get; set; } = "";
    }
}
