namespace Application.Contracts.Concept
{
    public class SaveConceptDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
}
