namespace Application.Models
{
    public class TopicResult
    {
        public string Id { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string Premise { get; set; } = default!;
        public string PremiseTr { get; set; } = default!;
        public string Tone { get; set; } = default!;
        public string PotentialVisual { get; set; } = default!;
        public bool NeedsFootage { get; set; }
        public bool FactCheck { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

}
