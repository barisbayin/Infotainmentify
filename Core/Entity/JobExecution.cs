using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class JobExecution : BaseEntity
    {
        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public JobSetting Job { get; set; } = null!;

        public JobStatus Status { get; set; } = JobStatus.Pending;

        [Column(TypeName = "nvarchar(max)")]
        public string ResultJson { get; set; } = "{}";

        [Column(TypeName = "nvarchar(max)")]
        public string? ErrorMessage { get; set; }

        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
