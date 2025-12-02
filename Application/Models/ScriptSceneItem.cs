namespace Application.Models
{
    public class ScriptSceneItem
    {
        public int SceneNumber { get; set; }
        public string VisualPrompt { get; set; } = default!; // Resim AI için tarif
        public string AudioText { get; set; } = default!;    // TTS için konuşma metni
        public int EstimatedDuration { get; set; } // Saniye
    }
}
