namespace Application.Models
{
    public class TopicStagePayload
    {
        public int TopicId { get; set; }
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicText { get; set; } = string.Empty;
        public string AudiencePromise { get; set; } = string.Empty;
        public string CentralQuestion { get; set; } = string.Empty;
        public string Angle { get; set; } = string.Empty;
        public List<string> KeyPoints { get; set; } = new();
        public List<string> ChapterHints { get; set; } = new();
        public string AvoidNotes { get; set; } = string.Empty;
        public ProductionBrief? Brief { get; set; }
        public string Language { get; set; } = "tr-TR";
    }
}
