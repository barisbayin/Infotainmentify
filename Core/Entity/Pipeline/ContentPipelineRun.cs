using Core.Entity.User;
using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity.Pipeline
{
    public class ContentPipelineRun : BaseEntity
    {
        [MaxLength(1000)]
        public string? RunContextTitle { get; set; }

        public string Language { get; set; } = "en-US";

        [Required]
        public int AppUserId { get; set; }
        // Navigation
        public AppUser AppUser { get; set; } = null!;

        // Hangi reçeteyi (Template) uyguluyoruz?
        [Required]
        public int TemplateId { get; set; }
        // Navigation
        public ContentPipelineTemplate Template { get; set; } = null!;

        // Durum: Pending -> Running -> Completed/Failed
        [Required]
        public ContentPipelineStatus Status { get; set; } = ContentPipelineStatus.Pending;

        // Performans takibi
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Eğer pipeline patlarsa, hatanın özeti burada duracak.
        // Detaylar ise StageExecution içindeki loglarda olacak.
        public string? ErrorMessage { get; set; }

        public bool AutoPublish { get; set; }

        // ==========================================================
        // ALT ELEMANLAR (Detaylı Loglar)
        // ==========================================================
        // Bu üretim emrinin altındaki her bir adımın canlı kaydı.
        public ICollection<StageExecution> StageExecutions { get; set; } = new List<StageExecution>();
    }
}
