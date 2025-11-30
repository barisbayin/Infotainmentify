namespace Application.Contracts.Concept
{
    public class ConceptDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
