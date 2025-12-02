namespace Application.Models
{
    public class ScriptStagePayload
    {
        public int ScriptId { get; set; }
        public string Title { get; set; } = default!;
        public string FullScriptText { get; set; } = default!; // Okumak için düz metin

        // Video üretimi için kritik olan kısım:
        public List<ScriptSceneItem> Scenes { get; set; } = new();
    }
}
