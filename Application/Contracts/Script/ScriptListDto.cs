namespace Application.Contracts.Story
{
    public class ScriptListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? TopicTitle { get; set; } // Join ile doldurulacak
        public int EstimatedDurationSec { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
