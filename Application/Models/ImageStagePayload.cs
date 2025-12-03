namespace Application.Models
{
    public class ImageStagePayload
    {
        public int ScriptId { get; set; }

        // Sahneler ve üretilen resimlerin yolları
        public List<SceneImageItem> SceneImages { get; set; } = new();
    }
}
