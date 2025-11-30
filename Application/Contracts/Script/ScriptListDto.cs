namespace Application.Contracts.Story
{
    // LIST (Grid için)
    public class ScriptListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? TopicTitle { get; set; } // Topic'ten gelen başlık
        public int EstimatedDurationSec { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
