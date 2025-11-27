namespace Application.Models
{

    public class WordTimestamp
    {
        public string Word { get; set; } = default!;
        public double Start { get; set; }   // seconds
        public double End { get; set; }     // seconds
        public double? Confidence { get; set; }
    }

}
