using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Presets
{
    public class ImagePreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int UserAiConnectionId { get; set; }
        public UserAiConnection UserAiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!; // Örn: "Shorts - Dark Fantasy"

        // Model: "dall-e-3", "stable-diffusion-xl", "leonardo-phoenix"
        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "dall-e-3";

        // ---------------------------------------------------------
        // GÖRSEL AYARLAR
        // ---------------------------------------------------------

        // Sanat Tarzı (UI'da göstermek veya prompt'a eklemek için)
        // Örn: "Cyberpunk", "Watercolor", "Photorealistic"
        [MaxLength(100)]
        public string? ArtStyle { get; set; }

        // Çözünürlük / Boyut
        // DALL-E 3 için: "1024x1024" (Square), "1024x1792" (Vertical/Shorts), "1792x1024" (Wide)
        [Required, MaxLength(20)]
        public string Size { get; set; } = "1024x1792";

        // Kalite ayarı (DALL-E'de var: "standard" veya "hd")
        [MaxLength(20)]
        public string Quality { get; set; } = "standard";

        // ---------------------------------------------------------
        // PROMPT MİMARİSİ
        // ---------------------------------------------------------

        /// <summary>
        /// Görsel üretim şablonu.
        /// Değişkenler: {SceneDescription} -> Script aşamasından gelen sahne tarifi.
        /// Örn: "{SceneDescription}, in the style of {ArtStyle}, highly detailed, 8k resolution, cinematic lighting"
        /// </summary>
        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = null!;

        /// <summary>
        /// Negatif Prompt (Nelerin olmamasını istiyoruz?)
        /// Stable Diffusion ve Leonardo için kritik. DALL-E bunu pek takmaz ama dursun.
        /// Örn: "ugly, deformed, blurry, low quality, text, watermark"
        /// </summary>
        [MaxLength(2000)]
        public string? NegativePrompt { get; set; }

        // Bir sahne için kaç varyasyon üretilsin? (Genelde 1, ama seçmece yapılacaksa artar)
        public int ImageCountPerScene { get; set; } = 1;
    }
}
