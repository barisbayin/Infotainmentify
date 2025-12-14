using Core.Entity.Models;

namespace Application.Models
{
    // Bu sınıf önceden sadece basit int/string tutuyordu.
    // Şimdi Preset içindeki detaylı objeleri taşıyacak.
    public class RenderStyleSettings
    {
        // 1. Teknik Ayarlar (FFmpeg Encoder)
        public int BitrateKbps { get; set; }
        public string EncoderPreset { get; set; } = "medium";

        // 2. Detaylı Ayar Grupları (Core katmanındaki modelleri direkt kullanıyoruz)
        public RenderCaptionSettings CaptionSettings { get; set; } = new();
        public RenderAudioMixSettings AudioMixSettings { get; set; } = new();
        public RenderVisualEffectsSettings VisualEffectsSettings { get; set; } = new();
        public RenderBrandingSettings BrandingSettings { get; set; } = new(); // 🔥 Yeni Watermark Ayarı
    }
}
