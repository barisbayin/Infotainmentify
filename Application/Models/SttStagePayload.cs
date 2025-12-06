namespace Application.Models
{
    public class SttStagePayload
    {
        public int ScriptId { get; set; }

        // Tüm videonun birleştirilmiş kelime listesi (Global Zamanlı)
        public List<SubtitleItem> Subtitles { get; set; } = new();
    }
}
