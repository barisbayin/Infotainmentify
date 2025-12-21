using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class Script : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        // --------------------------------------------------------
        // BAĞLANTILAR
        // --------------------------------------------------------

        // Hangi Topic'ten türetildi? (Opsiyonel olabilir, manuel de yazılabilir)
        public int? TopicId { get; set; }
        public Topic? Topic { get; set; }

        // Hangi Pipeline/Run üretti? (Audit)
        public int? CreatedByRunId { get; set; }

        // Hangi Preset kullanıldı?
        public int? SourcePresetId { get; set; }

        // --------------------------------------------------------
        // İÇERİK
        // --------------------------------------------------------

        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        // Düz metin hali (Okumak için)
        [Required]
        public string Content { get; set; } = default!;

        // ⚠️ KRİTİK: Video üretimi için sahneleme yapısı
        // JSON: [{ "scene": 1, "visualPrompt": "...", "audioText": "..." }, ...]
        public string? ScenesJson { get; set; }

        [MaxLength(10)]
        public string LanguageCode { get; set; } = "tr-TR";

        // Tahmini okuma/izleme süresi (Saniye)
        public int EstimatedDurationSec { get; set; }


        // 🔥 YENİ EKLENEN ALANLAR
        // ---------------------------------------------------------
        [MaxLength(1000)]
        public string? Description { get; set; } // Video açıklaması

        [MaxLength(1000)]
        public string? Tags { get; set; } // JSON formatında tutacağız: ["#shorts", "#ai"]
        // ---------------------------------------------------------
    }
}
