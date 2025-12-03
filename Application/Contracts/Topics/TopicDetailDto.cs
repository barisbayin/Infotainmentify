namespace Application.Contracts.Topics
{
    public class TopicDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Premise { get; set; } = default!;
        public string LanguageCode { get; set; } = default!;
        public int? ConceptId { get; set; }

        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Series { get; set; }
        public string? TagsJson { get; set; }

        public string? Tone { get; set; }
        public string? RenderStyle { get; set; }
        public string? VisualPromptHint { get; set; }

        public int? CreatedByRunId { get; set; }
        public int? SourcePresetId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
