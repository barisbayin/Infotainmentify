namespace Application.Contracts.Topics
{
    // 1. LIST DTO (Grid için hafif)
    public class TopicListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Premise { get; set; } = default!; // Gridde özet göstermek için
        public string? Category { get; set; }
        public string? Series { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
