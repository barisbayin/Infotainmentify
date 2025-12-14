namespace Application.Models
{
    public class RenderStyleSettings
    {
        // Altyazı
        public int FontSize { get; set; } = 30;
        public string FontColor { get; set; } = "&H00FFFFFF"; // Beyaz

        // Video Teknik
        public int BitrateKbps { get; set; } = 6000;
        public string EncoderPreset { get; set; } = "medium"; // ultrafast, medium, slow

        // Ses
        public double MusicVolume { get; set; } = 0.15; // 0.0 - 1.0 arası
        public bool IsDuckingEnabled { get; set; } = true;
    }
}
