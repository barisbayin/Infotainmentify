using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class ContentPipelineTemplate : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        [Required]
        public int ConceptId { get; set; }

        // Kullanıcının bu template'e verdiği isim:
        // "Shorts - Image + TTS", "Long Cinematic", "Podcast Conversion" vb.
        [Required, MaxLength(150)]
        public string Name { get; set; } = default!;

        // Template açıklaması / notlar
        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation
        public Concept Concept { get; set; } = default!;

        // Bu template'e bağlı stage adımları
        public List<StageConfig> StageConfigs { get; set; } = new();
    }
}
