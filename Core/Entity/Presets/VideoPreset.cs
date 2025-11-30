using Core.Entity.Models;
using Core.Entity.User;
using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Core.Entity.Presets
{
    public class VideoPreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int UserAiConnectionId { get; set; }
        public UserAiConnection UserAiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!; // "Cinematic Drone Shot - Runway"

        // ---------------------------------------------------------
        // MODEL & MOD
        // ---------------------------------------------------------

        // "runway-gen3", "luma-dream-machine", "kling-v1", "pika-art"
        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "runway-gen-3-alpha";

        // Text-to-Video mu yoksa Image-to-Video mu?
        // I2V seçilirse, bir önceki Image stage'inden gelen görseli canlandırır.
        [Required]
        public VideoGenerationMode GenerationMode { get; set; } = VideoGenerationMode.ImageToVideo;

        // ---------------------------------------------------------
        // ÇIKTI AYARLARI
        // ---------------------------------------------------------

        // 16:9, 9:16
        [Required, MaxLength(20)]
        public string AspectRatio { get; set; } = "9:16";

        // Kaç saniye olsun? (Genelde 5s veya 10s pahalıdır)
        public int DurationSeconds { get; set; } = 5;

        // ---------------------------------------------------------
        // PROMPT YÖNETİMİ
        // ---------------------------------------------------------

        /// <summary>
        /// Video Prompt Şablonu.
        /// Değişkenler: {SceneDescription}, {ImageStyle}
        /// Örn: "Cinematic drone shot of {SceneDescription}, highly detailed, 4k"
        /// </summary>
        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = null!;

        [MaxLength(2000)]
        public string? NegativePrompt { get; set; }

        // ---------------------------------------------------------
        // GELİŞMİŞ AYARLAR (JSON)
        // ---------------------------------------------------------

        // Kamera Hareketleri (Zoom, Pan, Tilt, Roll)
        // Runway ve Luma gibi modellerde kamera yönetimi çok kritiktir.
        public string? CameraControlSettingsJson { get; set; }

        // İleri Seviye Parametreler (Seed, Motion Bucket, Consistency)
        public string? AdvancedSettingsJson { get; set; }

        // ---------------------------------------------------------
        // HELPER PROPERTIES
        // ---------------------------------------------------------

        [NotMapped]
        public VideoCameraSettings CameraSettings
        {
            get => string.IsNullOrEmpty(CameraControlSettingsJson)
                   ? new VideoCameraSettings()
                   : JsonSerializer.Deserialize<VideoCameraSettings>(CameraControlSettingsJson)!;
            set => CameraControlSettingsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public VideoAdvancedSettings AdvancedSettings
        {
            get => string.IsNullOrEmpty(AdvancedSettingsJson)
                   ? new VideoAdvancedSettings()
                   : JsonSerializer.Deserialize<VideoAdvancedSettings>(AdvancedSettingsJson)!;
            set => AdvancedSettingsJson = JsonSerializer.Serialize(value);
        }
    }
}
