using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    /// <summary>
    /// Script = Topic'ten türetilmiş AI destekli veya manuel oluşturulmuş senaryo/metin içeriği.
    /// </summary>
    public class Script : BaseEntity
    {
        // 👤 Kullanıcıya aitlik
        [Required]
        public int UserId { get; set; }

        // 🧩 Bağlı olduğu Topic
        [Required]
        public int TopicId { get; set; }

        // 🔗 Hangi üretim profiliyle oluşturuldu
        public int? ScriptGenerationProfileId { get; set; }
        [ForeignKey(nameof(ScriptGenerationProfileId))]
        public ScriptGenerationProfile? ScriptGenerationProfile { get; set; }

        // 🤖 AI kaynak bilgisi
        public int? AiConnectionId { get; set; }
        [ForeignKey(nameof(AiConnectionId))]
        public UserAiConnection? AiConnection { get; set; }

        public int? PromptId { get; set; }
        [ForeignKey(nameof(PromptId))]
        public Prompt? Prompt { get; set; }

        // 📜 İçerik alanları
        [MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Content { get; set; } = default!;

        [MaxLength(200)]
        public string? Summary { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        // 🎨 Üretim stili bilgileri
        [MaxLength(50)]
        public string? RenderStyle { get; set; }

        [MaxLength(50)]
        public string? ProductionType { get; set; }

        // 🎛️ AI metadata: model adı, sıcaklık, response time vs.
        [Column(TypeName = "nvarchar(max)")]
        public string? MetaJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ScriptJson { get; set; }

        // 🧠 İstatistik / durum bilgileri
        public int? ResponseTimeMs { get; set; }

        // 🔗 Navigation
        public virtual Topic Topic { get; set; } = default!;
        public virtual AppUser User { get; set; } = default!;
    }
}
