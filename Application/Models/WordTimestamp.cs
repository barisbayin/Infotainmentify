namespace Application.Models
{
    public class WordTimestamp
    {
        public string Word { get; set; } = string.Empty;

        // Saniye cinsinden başlangıç (Örn: 1.5)
        public double Start { get; set; }

        // Saniye cinsinden bitiş (Örn: 1.9)
        public double End { get; set; }

        // Doğruluk oranı (0.0 - 1.0)
        public double Confidence { get; set; }
    }
}
