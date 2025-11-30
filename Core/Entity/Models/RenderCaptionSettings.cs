using Core.Enums;

namespace Core.Entity.Models
{
    public class RenderCaptionSettings
    {
        public bool EnableCaptions { get; set; } = true;

        // Stil İsmi (Frontend'de hazır stiller seçtirmek için)
        public string StyleName { get; set; } = "mrbeast_glow";

        public string FontName { get; set; } = "Arial-Bold";
        public int FontSize { get; set; } = 48;

        // Renkler (Hex)
        public string PrimaryColor { get; set; } = "#FFFFFF";
        public string OutlineColor { get; set; } = "#000000";
        public int OutlineSize { get; set; } = 4;

        // Vurgulama (Karaoke)
        public bool EnableHighlight { get; set; } = true;
        public string HighlightColor { get; set; } = "#FFFF00"; // Sarı vurgu

        // Animasyon
        public CaptionAnimationTypes Animation { get; set; } = CaptionAnimationTypes.PopUp;

        // Yerleşim
        public CaptionPositionTypes Position { get; set; } = CaptionPositionTypes.Center;
        public int MarginBottom { get; set; } = 100; // Alttan boşluk

        // Kelime Gruplama
        public int MaxWordsPerLine { get; set; } = 2; // Hızlı okuma için az kelime
        public bool Uppercase { get; set; } = true; // HEPSİ BÜYÜK HARF
    }
}
