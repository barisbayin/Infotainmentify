namespace Application.Models
{
    public class TopicStagePayload
    {
        public int TopicId { get; set; }
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicText { get; set; } = string.Empty;
        public string Language { get; set; } = "tr-TR";
    }
}
