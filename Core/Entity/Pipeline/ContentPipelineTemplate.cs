using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Pipeline
{
    public class ContentPipelineTemplate : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        // Navigation: Sahibini tanıyalım
        public AppUser AppUser { get; set; } = null!;

        [Required]
        public int ConceptId { get; set; }
        // Navigation: Hangi markanın/kanalın işi?
        public Concept Concept { get; set; } = null!;

        // "Shorts - Image + TTS", "Long Form - Video AI"
        [Required, MaxLength(150)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool AutoPublish { get; set; } = false;

        // ==========================================
        // ALT ELEMANLAR (Malzemeler ve Çıktılar)
        // ==========================================

        // 1. TARİFİN ADIMLARI (Çok Önemli)
        // Bu şablonun içinde hangi stage'ler var? (Topic -> Script -> Image...)
        public ICollection<StageConfig> StageConfigs { get; set; } = new List<StageConfig>();

        // 2. ÜRETİM GEÇMİŞİ (Analytics)
        // Bu şablon kullanılarak üretilen tüm pipeline'lar (Loglar/Geçmiş)
        public ICollection<ContentPipelineRun> Runs { get; set; } = new List<ContentPipelineRun>();
    }
}
