namespace Core.Entity.Models
{
    public class RenderBrandingSettings
    {
        public bool EnableWatermark { get; set; } = false;

        // Ekranda yazacak metin (örn: @KanalAdi)
        public string WatermarkText { get; set; } = "";

        // Veya Resim yolu (örn: /assets/logo.png)
        public string? WatermarkImagePath { get; set; }

        public string WatermarkColor { get; set; } = "#FFFFFF";
        public double Opacity { get; set; } = 0.5; // Yarı saydam

        // Konum (TopLeft, BottomRight vs.) - İleride enum yapılabilir
        public string Position { get; set; } = "BottomRight";
    }
}
