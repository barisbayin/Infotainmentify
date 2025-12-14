using Core.Entity.Models;
using Core.Entity.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Core.Entity.Presets
{
    public class RenderPreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!; // "Shorts - Viral MrBeast", "Long - Documentary"

        // -------------------------------------------------
        // 1. VİDEO TEKNİK ÖZELLİKLERİ (FFmpeg Core)
        // -------------------------------------------------

        // Genişlik ve Yükseklik (String "1080x1920" yerine ayrı tutmak matematik için daha iyidir)
        public int OutputWidth { get; set; } = 1080;  // Shorts: 1080, Long: 1920
        public int OutputHeight { get; set; } = 1920; // Shorts: 1920, Long: 1080

        public int Fps { get; set; } = 30; // 24, 30, 60

        // Video Kalitesi (Bitrate - kbps). 
        // Shorts için 5000-8000 arası iyidir. 4K için 15000+.
        public int BitrateKbps { get; set; } = 6000;

        // FFmpeg Preset (Render hızı vs Kalite dengesi)
        // "ultrafast", "fast", "medium", "slow" (Yüksek kalite için slow)
        [MaxLength(20)]
        public string EncoderPreset { get; set; } = "medium";

        // Video Formatı
        [MaxLength(10)]
        public string ContainerFormat { get; set; } = "mp4"; // mp4, mov, webm

        // -------------------------------------------------
        // 2. DETAYLI AYARLAR (JSON OLARAK SAKLANIR)
        // -------------------------------------------------

        // Altyazı Ayarları (Font, Renk, Konum, Animasyon)
        public string? CaptionSettingsJson { get; set; }

        // Ses Miksaj Ayarları (Müzik seviyesi, Ducking, Fade)
        public string? AudioMixSettingsJson { get; set; }

        // Görsel Efektler (Zoom, Pan, Transition)
        public string? VisualEffectsSettingsJson { get; set; }

        // Intro / Outro / Watermark (Markalama)
        public string? BrandingSettingsJson { get; set; }

        // -------------------------------------------------
        // HELPER PROPERTIES (Kod içinde JSON ile uğraşmamak için)
        // -------------------------------------------------

        [NotMapped]
        public RenderCaptionSettings CaptionSettings
        {
            get => string.IsNullOrEmpty(CaptionSettingsJson)
                   ? new RenderCaptionSettings()
                   : JsonSerializer.Deserialize<RenderCaptionSettings>(CaptionSettingsJson)!;
            set => CaptionSettingsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public RenderAudioMixSettings AudioMixSettings
        {
            get => string.IsNullOrEmpty(AudioMixSettingsJson)
                   ? new RenderAudioMixSettings()
                   : JsonSerializer.Deserialize<RenderAudioMixSettings>(AudioMixSettingsJson)!;
            set => AudioMixSettingsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public RenderVisualEffectsSettings VisualEffectsSettings
        {
            get => string.IsNullOrEmpty(VisualEffectsSettingsJson)
                   ? new RenderVisualEffectsSettings()
                   : JsonSerializer.Deserialize<RenderVisualEffectsSettings>(VisualEffectsSettingsJson)!;
            set => VisualEffectsSettingsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public RenderBrandingSettings BrandingSettings // 🔥 YENİ
        {
            get => DeserializeOrNew<RenderBrandingSettings>(BrandingSettingsJson);
            set => BrandingSettingsJson = JsonSerializer.Serialize(value);
        }

        // Helper
        private T DeserializeOrNew<T>(string? json) where T : new()
            => string.IsNullOrEmpty(json) ? new T() : JsonSerializer.Deserialize<T>(json)!;
    }
}
