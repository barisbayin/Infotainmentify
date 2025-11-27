using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class Concept : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        // Magic Tiny Stories, Mysteryify, Infotainmentify Facts, vs.
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        // Kullanıcının bu konsept için yazdığı kısa açıklama
        [MaxLength(300)]
        public string? Description { get; set; }
    }
}
