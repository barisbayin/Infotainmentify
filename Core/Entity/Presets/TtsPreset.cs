using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Presets
{
    public class TtsPreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int UserAiConnectionId { get; set; }
        public UserAiConnection UserAiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!; // Örn: "Shorts - Adam Voice"

        // ---------------------------------------------------------
        // SES MOTORU AYARLARI
        // ---------------------------------------------------------

        // Seslendirmeyi yapacak kişinin/robotun ID'si
        // ElevenLabs için: "21m00Tcm4TlvDq8ikWAM" (Rachel)
        // Google için: "en-US-Neural2-J"
        // OpenAI için: "alloy"
        [Required, MaxLength(100)]
        public string VoiceId { get; set; } = default!;

        // Hangi dil? (Google buna ihtiyaç duyar, ElevenLabs modelden anlar ama tutmak iyidir)
        [MaxLength(20)]
        public string LanguageCode { get; set; } = "tr-TR"; // "en-US", "tr-TR"

        // Model / Engine Adı
        // ElevenLabs: "eleven_multilingual_v2"
        // Google: "neural"
        // OpenAI: "tts-1-hd"
        [MaxLength(100)]
        public string? EngineModel { get; set; }

        // ---------------------------------------------------------
        // PERFORMANS / TONLAMA AYARLARI
        // ---------------------------------------------------------

        // Konuşma Hızı (1.0 = Normal, 1.2 = Hızlı)
        // Shorts videoları için genelde 1.15x veya 1.2x tercih edilir.
        public double SpeakingRate { get; set; } = 1.0;

        // Ses Tonu / Perde (0.0 = Normal)
        // Google destekler, ElevenLabs genelde desteklemez.
        public double Pitch { get; set; } = 0.0;

        // Denge / Kararlılık (ElevenLabs Stability)
        // Düşük olursa daha duygusal/rastgele, yüksek olursa daha robotik/stabil.
        // 0.0 - 1.0 arası
        public double Stability { get; set; } = 0.5;

        // Sesin netliği / benzerliği (ElevenLabs Similarity Boost)
        // 0.0 - 1.0 arası
        public double Clarity { get; set; } = 0.75;

        // Stil abartısı (ElevenLabs Style Exaggeration)
        public double StyleExaggeration { get; set; } = 0.0;
    }
}
