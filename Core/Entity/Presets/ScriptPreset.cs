using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Presets
{
    public class ScriptPreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int UserAiConnectionId { get; set; }
        public UserAiConnection UserAiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!; // Örn: "Shorts - Viral Style"

        // Model (Script için farklı bir model kullanabilirsin, örn: Claude 3 Opus)
        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "gpt-4-turbo";

        // ---------------------------------------------------------
        // SENARYO AYARLARI
        // ---------------------------------------------------------

        // Tonlama: "Humorous", "Dark", "Educational", "Sarcastic"
        [MaxLength(50)]
        public string Tone { get; set; } = "Engaging";

        // Hedeflenen uzunluk (Saniye cinsinden tahmini)
        // Prompt'a "Write a script for a 60 second video" diyeceğiz.
        public int TargetDurationSec { get; set; } = 60;

        // Dil (Topic'ten farklı bir dilde script yazdırmak istersen override edebilirsin)
        [MaxLength(10)]
        public string Language { get; set; } = "tr-TR";

        // ---------------------------------------------------------
        // PROMPT & FORMAT
        // ---------------------------------------------------------

        // Hook (Kanca) cümlesi istiyor muyuz? (Video başı için kritik)
        public bool IncludeHook { get; set; } = true;

        // "Call to Action" (Abone ol vs.) istiyor muyuz?
        public bool IncludeCta { get; set; } = true;

        /// <summary>
        /// Ana senaryo prompt şablonu.
        /// Değişkenler: {Topic}, {Tone}, {Duration}, {Language}
        /// Örn: "Write a YouTube Shorts script about {Topic}. Use a {Tone} tone..."
        /// </summary>
        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = null!;

        // Ekstra talimatlar (System Prompt)
        [MaxLength(2000)]
        public string? SystemInstruction { get; set; }
    }
}
