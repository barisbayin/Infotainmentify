namespace Application.Contracts.Topics
{
    public sealed class TopicListDto
    {
        public int Id { get; set; }
        public string? TopicCode { get; set; }
        public string? Category { get; set; }
        public string? Tone { get; set; }
        public bool NeedsFootage { get; set; }
        public bool FactCheck { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
