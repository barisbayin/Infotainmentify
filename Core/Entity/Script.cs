using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class Script : BaseEntity
    {
        // 👤 Kullanıcıya aitlik
        public int UserId { get; set; }

        // 🧩 Bağlı olduğu Topic
        public int TopicId { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Content { get; set; } = default!;

        [MaxLength(200)]
        public string? Summary { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        // 🎛️ AI metadata: model adı, sıcaklık, response time vs.
        public string? MetaJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ScriptJson { get; set; }

        // 🔗 Navigation
        public virtual Topic Topic { get; set; } = default!;
        public virtual AppUser User { get; set; } = default!;
    }
}
