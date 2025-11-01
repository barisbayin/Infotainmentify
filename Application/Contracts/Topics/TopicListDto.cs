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
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Liste ekranında prompt adı gösterebilmek için
        public int? PromptId { get; set; }
        public string? PromptTitle { get; set; }

        public string? PremiseTr { get; set; }
        public string? Premise { get; set; }
    }
}
