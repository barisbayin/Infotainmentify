namespace Application.Models
{
    public class SubtitleItem
    {
        public string Word { get; set; } = default!;
        public double Start { get; set; } // Global Başlangıç (sn)
        public double End { get; set; }   // Global Bitiş (sn)
        public double Confidence { get; set; }
        public int SceneNumber { get; set; } // Hangi sahneye ait olduğu
    }
}
