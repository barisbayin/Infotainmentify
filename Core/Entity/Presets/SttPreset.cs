using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Presets
{
    public class SttPreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int UserAiConnectionId { get; set; }
        public UserAiConnection UserAiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!; // Örn: "Whisper - Karaoke Mode"

        // ---------------------------------------------------------
        // MODEL AYARLARI
        // ---------------------------------------------------------

        // Model: "whisper-1", "nova-2" (Deepgram), "chirp" (Google)
        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "whisper-1";

        // Kaynak sesin dili. "auto" dersen AI kendisi algılar.
        [MaxLength(10)]
        public string LanguageCode { get; set; } = "auto"; // "tr", "en", "auto"

        // ---------------------------------------------------------
        // ÇIKTI FORMATI & DETAYLAR
        // ---------------------------------------------------------

        /// <summary>
        /// Kelime bazlı zaman damgası istiyor muyuz?
        /// TRUE ise: Her kelimenin start/end süresi döner. (Dinamik caption için ŞART)
        /// FALSE ise: Sadece düz metin veya cümle bazlı döner.
        /// </summary>
        public bool EnableWordLevelTimestamps { get; set; } = true;

        /// <summary>
        /// Konuşmacı ayrımı yapılsın mı? (Diarization)
        /// Podcast gibi birden fazla kişinin konuştuğu içerikler için.
        /// </summary>
        public bool EnableSpeakerDiarization { get; set; } = false;

        /// <summary>
        /// Çıktı formatı tercihi.
        /// "json" (tavsiye edilen, işlemeye uygun), "srt", "vtt", "text"
        /// </summary>
        [MaxLength(10)]
        public string OutputFormat { get; set; } = "json";

        // ---------------------------------------------------------
        // İNCE AYARLAR
        // ---------------------------------------------------------

        // Whisper'a özel: Metnin stilini veya özel isimleri öğretmek için ipucu.
        // Örn: "Infotainmentify, C#, .NET Core"
        [MaxLength(1000)]
        public string? Prompt { get; set; }

        // 0.0 ile 1.0 arası. 0'a yakınsa daha deterministik, 1'e yakınsa daha yaratıcı (ama riskli).
        public double Temperature { get; set; } = 0.0;

        // Küfür/Argo filtresi (Marka güvenliği için)
        public bool FilterProfanity { get; set; } = false;
    }
}
