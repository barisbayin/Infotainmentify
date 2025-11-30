namespace Core.Entity.Models
{
    public class VideoCameraSettings
    {
        // Kamera Hareketi
        // Değerler genelde -10 ile +10 arasındadır.
        public double Zoom { get; set; } = 0.0; // + ileri, - geri

        public double PanX { get; set; } = 0.0; // Sağa/Sola kaydırma
        public double PanY { get; set; } = 0.0; // Yukarı/Aşağı kaydırma

        public double Tilt { get; set; } = 0.0; // Kamerayı yukarı/aşağı eğme
        public double Roll { get; set; } = 0.0; // Kamerayı döndürme

        // Sadece belirli modeller destekler (Örn: Runway)
        public string? CameraMotionDescription { get; set; } // "Zoom in fast"
    }
}
