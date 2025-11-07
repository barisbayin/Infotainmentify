using Core.Abstractions;
using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class ScriptGenerationProfile : BaseEntity, IJobProfile
    {
        [Required]
        public int AppUserId { get; set; }
        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        // 🔗 Optional: Hangi Topic batch'inden script üretileceğini belirtir
        public int? TopicGenerationProfileId { get; set; }
        [ForeignKey(nameof(TopicGenerationProfileId))]
        public TopicGenerationProfile? TopicGenerationProfile { get; set; }

        // 📜 Kullanılacak prompt (zorunlu)
        [Required]
        public int PromptId { get; set; }
        [ForeignKey(nameof(PromptId))]
        public Prompt Prompt { get; set; } = null!;

        // 🤖 AI bağlantısı (zorunlu)
        [Required]
        public int AiConnectionId { get; set; }
        [ForeignKey(nameof(AiConnectionId))]
        public UserAiConnection AiConnection { get; set; } = null!;

        // 💡 Profil bilgileri
        [Required, MaxLength(100)]
        public string ProfileName { get; set; } = null!;

        [Required, MaxLength(50)]
        public string ModelName { get; set; } = null!;

        public double Temperature { get; set; } = 0.8;

        [MaxLength(10)]
        public string? Language { get; set; } = "en";

        // 🔄 Eğer belirli topic’ler seçildiyse burada saklanır (örn: [1,4,7])
        [Column(TypeName = "nvarchar(max)")]
        public string? TopicIdsJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ConfigJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? RawResponseJson { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(32)]
        public string? ProductionType { get; set; } // "video" | "image" | "audio" | "mixed"
        [MaxLength(64)]
        public string? RenderStyle { get; set; }    // "cinematic_vertical" | "fastcut_info" | ...

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? CompletedAt { get; set; }

        // 🧩 IJobProfile Implementation
        public JobType JobType => JobType.ScriptGeneration;

        public IDictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "PromptId", PromptId },
                { "AiConnectionId", AiConnectionId },
                { "ModelName", ModelName },
                { "Temperature", Temperature },
                { "Language", Language ?? "en" },
                { "TopicGenerationProfileId", TopicGenerationProfileId ?? 0 },
                { "TopicIdsJson", TopicIdsJson ?? "[]" }
            };
        }

        public void Validate()
        {
            if (PromptId <= 0)
                throw new InvalidOperationException("Prompt zorunludur.");
            if (AiConnectionId <= 0)
                throw new InvalidOperationException("AI bağlantısı zorunludur.");
        }
    }
}
