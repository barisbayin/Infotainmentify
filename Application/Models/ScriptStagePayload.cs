namespace Application.Models
{
    public class ScriptStagePayload
    {
        public int ScriptId { get; set; }
        public string Title { get; set; } = default!;
        public string FullScriptText { get; set; } = default!; // Okumak için düz metin

        // Video Açıklaması (YouTube description) - EKLENDİ
        public string Description { get; set; } = default!;

        // Video Etiketleri/Hashtagleri - EKLENDİ
        public List<string> Tags { get; set; } = new();

        // Video üretimi için kritik olan kısım:
        public List<ScriptSceneItem> Scenes { get; set; } = new();
    }
}
