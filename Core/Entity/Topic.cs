using Core.Entity.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class Topic : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        // --------------------------------------------------------
        // KÖKEN (Provenance) - Bu fikir nereden geldi?
        // --------------------------------------------------------

        // Hangi Pipeline Run sırasında üretildi? (Audit için)
        public int? CreatedByRunId { get; set; }

        // Hangi Preset (Ayarlar) kullanıldı?
        public int? SourcePresetId { get; set; }

        // --------------------------------------------------------
        // TEMEL İÇERİK
        // --------------------------------------------------------

        // Kısa başlık / Kod yerine ID kullanacağız ama slug tutabilirsin.
        [Required, MaxLength(1000)]
        public string Title { get; set; } = default!;

        // Ana Fikir (İngilizce veya Türkçe fark etmez, LanguageCode belirler)
        // Eskiden PremiseTr vardı, artık tek alan var.
        [Required]
        public string Premise { get; set; } = default!;

        [MaxLength(10)]
        public string LanguageCode { get; set; } = "tr-TR";

        // --------------------------------------------------------
        // KATEGORİZASYON (Filtreleme İçin)
        // --------------------------------------------------------

        [MaxLength(64)]
        public string? Category { get; set; }      // "Science", "History"

        [MaxLength(128)]
        public string? SubCategory { get; set; }   // "Space", "WW2"

        [MaxLength(128)]
        public string? Series { get; set; }        // "Did You Know?"

        // JSON Array: ["viral", "shocking", "education"]
        public string? TagsJson { get; set; }

        // --------------------------------------------------------
        // YAPIMCI NOTLARI (AI Tarafından Önerilen)
        // Script ve Image aşamaları bu ipuçlarını kullanır.
        // --------------------------------------------------------

        [MaxLength(64)]
        public string? Tone { get; set; }          // "Mysterious", "Upbeat"

        [MaxLength(64)]
        public string? RenderStyle { get; set; }   // "Cinematic", "PixelArt"

        [MaxLength(4000)]
        public string? VisualPromptHint { get; set; } // AI'ın görsel önerisi ("Dark forest with fog")

        // --------------------------------------------------------
        // HAM VERİ (Fallback)
        // --------------------------------------------------------

        // AI'dan dönen tüm JSON cevabını buraya yedekleyelim.
        // İleride extra bir alan lazım olursa buradan parse ederiz.
        public string? RawJsonData { get; set; }
    }
}