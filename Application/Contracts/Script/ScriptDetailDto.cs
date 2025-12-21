namespace Application.Contracts.Script
{
    public class ScriptDetailDto
    {
        public int Id { get; set; }
        public int? TopicId { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string? ScenesJson { get; set; }
        public string LanguageCode { get; set; } = default!;
        public int EstimatedDurationSec { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; } // Video açıklaması
        public string? Tags { get; set; }
    }
}
