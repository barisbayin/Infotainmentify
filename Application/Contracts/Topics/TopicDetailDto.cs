namespace Application.Contracts.Topics
{
    public sealed class TopicDetailDto
    {
        public int Id { get; set; }
        public string? TopicCode { get; set; }
        public string? Category { get; set; }
        public string? PremiseTr { get; set; }
        public string? Premise { get; set; }
        public string? Tone { get; set; }
        public string? PotentialVisual { get; set; }
        public bool NeedsFootage { get; set; }
        public bool FactCheck { get; set; }
        public string? TagsJson { get; set; }
        public string? TopicJson { get; set; }
        public int? PromptId { get; set; }
        public bool IsActive { get; set; }
    }
}
