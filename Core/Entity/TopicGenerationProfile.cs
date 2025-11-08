using Core.Abstractions;
using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    /// <summary>
    /// TopicGenerationProfile = AI tabanlı topic üretiminin tarif (şablon) profilidir.
    /// Hangi model, prompt, bağlantı ve parametrelerle üretim yapılacağını tanımlar.
    /// Gerçek üretim, TopicGenerationService tarafından yapılır.
    /// </summary>
    public class TopicGenerationProfile : BaseEntity, IJobProfile
    {
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        [Required]
        public int PromptId { get; set; }

        [ForeignKey(nameof(PromptId))]
        public Prompt Prompt { get; set; } = null!;

        [Required]
        public int AiConnectionId { get; set; }

        [ForeignKey(nameof(AiConnectionId))]
        public UserAiConnection AiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ProfileName { get; set; } = null!; // Kullanıcıya özel ad (örn: “Science Shorts 30’luk Set”)

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = null!; // Örn: gpt-4-turbo, gemini-1.5-pro

        public int RequestedCount { get; set; } = 30; // Kaç topic üretilecek

        [MaxLength(50)]
        public string? ProductionType { get; set; }   // Örn: "infotainment", "horror", "science"

        [MaxLength(50)]
        public string? RenderStyle { get; set; }      // Örn: "cinematic", "handdrawn", "cartoon"

        [MaxLength(10)]
        public string Language { get; set; } = "en";  // "en", "tr", "es" gibi

        public float Temperature { get; set; } = 0.7f;

        public int? MaxTokens { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? TagsJson { get; set; } // JSON array: ["psychology","funny","viral"]

        [MaxLength(20)]
        public string OutputMode { get; set; } = "Topic"; // Topic | Script | Image | Mixed

        public bool AutoGenerateScript { get; set; } = false; // Topic sonrası otomatik script üretimi

        // --- Flags ---
        public bool IsPublic { get; set; } = false; // Başkaları da kullanabilsin mi?
        public bool AllowRetry { get; set; } = true; // Başarısız olursa tekrar dene

        public JobType JobType => JobType.TopicGeneration;

        public IDictionary<string, object> ToParameters()
        {
            throw new NotImplementedException();
        }

        public void Validate()
        {
            throw new NotImplementedException();
        }
    }
}
