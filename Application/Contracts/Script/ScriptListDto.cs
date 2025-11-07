namespace Application.Contracts.Story
{
    public  class ScriptListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Summary { get; set; }
        public string? Language { get; set; }
        public string? TopicCode { get; set; }
        public int? TopicId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
