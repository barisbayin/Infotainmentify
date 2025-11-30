using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Presets
{
    /// <summary>
    /// TopicPreset: Konu üretim aşaması için gerekli tüm ayarları tutar.
    /// Executor bu sınıfı okuyarak AI'ya nasıl bir istek atacağını bilir.
    /// </summary>
    public class TopicPreset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        // Navigation (BaseEntityConfig'de tanımlı ama explicit olması iyidir)
        public AppUser AppUser { get; set; } = null!;

        // ---------------------------------------------------------
        // 1. KİMLİK & BAĞLANTI
        // ---------------------------------------------------------

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!; // Örn: "Korku Hikayeleri - GPT4"

        [MaxLength(500)]
        public string? Description { get; set; }

        // Bu Preset hangi AI servisini kullanacak?
        [Required]
        public int UserAiConnectionId { get; set; }

        public UserAiConnection UserAiConnection { get; set; } = null!;

        // ---------------------------------------------------------
        // 2. AI PARAMETRELERİ
        // ---------------------------------------------------------

        // Model adı Connection içinde değil, burada olmalı demiştik.
        // Örn: "gpt-4o", "gemini-1.5-pro", "claude-3-opus"
        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "gpt-3.5-turbo";

        public float Temperature { get; set; } = 0.7f;

        // Çıktı hangi dilde olsun?
        [MaxLength(10)]
        public string Language { get; set; } = "tr-TR";

        // ---------------------------------------------------------
        // 3. PROMPT YAPISI
        // ---------------------------------------------------------

        /// <summary>
        /// AI'ya gönderilecek ana komut şablonu.
        /// Örn: "Sen yaratıcı bir yazarsın. Bana {Category} alanında, {Style} tonunda ilginç bir konu bul."
        /// </summary>
        [Required, MaxLength(5000)] // Uzun promptlar için yer açtık
        public string PromptTemplate { get; set; } = null!;

        /// <summary>
        /// Prompt içindeki dinamik alanları doldurmak veya AI'ya bağlam vermek için etiketler.
        /// JSON Array: ["history", "war", "sad"]
        /// Executor bunu prompt'un sonuna ekleyebilir.
        /// </summary>
        public string? ContextKeywordsJson { get; set; }

        // İleride "System Instruction" (Sen bir tarihçisin vb.) ayrılabilir.
        [MaxLength(2000)]
        public string? SystemInstruction { get; set; }
    }
}
