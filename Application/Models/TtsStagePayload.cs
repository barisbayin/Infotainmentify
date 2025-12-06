namespace Application.Models
{
    public class TtsStagePayload
    {
        public int ScriptId { get; set; }

        // Üretilen ses dosyalarının listesi
        public List<SceneAudioItem> SceneAudios { get; set; } = new();
    }
}
