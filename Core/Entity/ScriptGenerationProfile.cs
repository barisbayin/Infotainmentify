using Core.Abstractions;
using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    /// <summary>
    /// ScriptGenerationProfile = AI destekli script (senaryo/metin) üretim profili.
    /// Belirli topic veya topic setlerinden script oluşturmak için kullanılır.
    /// Ayrıca bağlı asset üretim (görsel, ses, video) AI sağlayıcılarını da tanımlar.
    /// </summary>
    public class ScriptGenerationProfile : BaseEntity, IJobProfile
    {
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        public int? TopicGenerationProfileId { get; set; }

        [ForeignKey(nameof(TopicGenerationProfileId))]
        public TopicGenerationProfile? TopicGenerationProfile { get; set; }

        [Required]
        public int PromptId { get; set; }

        [ForeignKey(nameof(PromptId))]
        public Prompt Prompt { get; set; } = null!;

        [Required]
        public int AiConnectionId { get; set; }

        [ForeignKey(nameof(AiConnectionId))]
        public UserAiConnection AiConnection { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ProfileName { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = null!;

        public float Temperature { get; set; } = 0.8f;

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        [MaxLength(20)]
        public string OutputMode { get; set; } = "Script";

        [Column(TypeName = "nvarchar(max)")]
        public string? ConfigJson { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [MaxLength(50)]
        public string? ProductionType { get; set; }

        [MaxLength(50)]
        public string? RenderStyle { get; set; }

        public bool IsPublic { get; set; } = false;
        public bool AllowRetry { get; set; } = true;

        // --------------------------------------------------------------------
        // 🧩 Yeni Alanlar — Asset & Video Generation Settings
        // --------------------------------------------------------------------

        // 🎨 Image Generation
        public int? ImageAiConnectionId { get; set; }

        [ForeignKey(nameof(ImageAiConnectionId))]
        public UserAiConnection? ImageAiConnection { get; set; }

        [MaxLength(100)]
        public string? ImageModelName { get; set; }

        [MaxLength(50)]
        public string? ImageRenderStyle { get; set; }

        [MaxLength(20)]
        public string? ImageAspectRatio { get; set; } = "16:9";

        // 🗣️ TTS Generation
        public int? TtsAiConnectionId { get; set; }

        [ForeignKey(nameof(TtsAiConnectionId))]
        public UserAiConnection? TtsAiConnection { get; set; }

        [MaxLength(100)]
        public string? TtsModelName { get; set; }

        [MaxLength(50)]
        public string? TtsVoice { get; set; }

        // 🎬 Video Render
        public int? VideoAiConnectionId { get; set; }

        [ForeignKey(nameof(VideoAiConnectionId))]
        public UserAiConnection? VideoAiConnection { get; set; }

        [MaxLength(100)]
        public string? VideoModelName { get; set; }

        [MaxLength(100)]
        public string? VideoTemplate { get; set; }

        // 🔄 Automation Flags
        public bool AutoGenerateAssets { get; set; } = false;
        public bool AutoRenderVideo { get; set; } = false;

        // --------------------------------------------------------------------
        // Interface Implementation
        // --------------------------------------------------------------------

        public JobType JobType => JobType.ScriptGeneration;

        public IDictionary<string, object> ToParameters() =>
            new Dictionary<string, object>
            {
                { "PromptId", PromptId },
                { "AiConnectionId", AiConnectionId },
                { "ModelName", ModelName },
                { "Temperature", Temperature },
                { "Language", Language },
                { "OutputMode", OutputMode },
                { "AutoGenerateAssets", AutoGenerateAssets },
                { "AutoRenderVideo", AutoRenderVideo },
                { "ImageAiConnectionId", ImageAiConnectionId ?? 0 },
                { "TtsAiConnectionId", TtsAiConnectionId ?? 0 },
                { "VideoAiConnectionId", VideoAiConnectionId ?? 0 },
            };

        public void Validate()
        {
            if (PromptId <= 0)
                throw new InvalidOperationException("Prompt zorunludur.");
            if (AiConnectionId <= 0)
                throw new InvalidOperationException("AI bağlantısı zorunludur.");
        }
    }
}
