using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class JobSetting : BaseEntity
    {
        [Required]
        public JobType JobType { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        // Profile bilgisi (örneğin TopicGenerationProfile.Id)
        public int ProfileId { get; set; }

        [MaxLength(300)]
        public string ProfileType { get; set; } = null!; // örn: typeof(TopicGenerationProfile).AssemblyQualifiedName

        // Schedule ayarları
        public bool IsAutoRunEnabled { get; set; }
        public decimal? PeriodHours { get; set; } // 0.5 = 30dk, 24 = 1 gün
        public DateTimeOffset? LastRunAt { get; set; }

        public JobStatus Status { get; set; } = JobStatus.Pending;

        [MaxLength(1000)]
        public string? LastError { get; set; }

        public DateTimeOffset? LastErrorAt { get; set; }
    }

}
