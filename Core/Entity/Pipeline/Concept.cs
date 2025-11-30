using Core.Entity.User;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Pipeline
{
    public class Concept : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        // Navigation Property: Üst Ebeveyn (Kullanıcı)
        public AppUser AppUser { get; set; } = null!;

        // Marka/Kanal Adı: "Magic Tiny Stories", "History Facts"
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        // Kullanıcının notları
        [MaxLength(300)]
        public string? Description { get; set; }

        // ==========================================================
        // YENİ EKLENEN KISIM: ALT ELEMANLAR (Templates)
        // ==========================================================

        // Bir konseptin altında birden fazla "Tarif" (Template) olabilir.
        // Örn: "History Facts" altında -> "Shorts (Dikey)" ve "Long Form (Yatay)"
        public ICollection<ContentPipelineTemplate> Templates { get; set; } = new List<ContentPipelineTemplate>();
    }
}
