using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class AutoVideoAsset : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        [Required]
        public int ProfileId { get; set; }   // AutoVideoAssetProfile

        public int? TopicId { get; set; }
        public int? ScriptId { get; set; }

        [MaxLength(500)]
        public string? VideoPath { get; set; }

        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }

        public bool Uploaded { get; set; }

        [MaxLength(200)]
        public string? UploadVideoId { get; set; }

        [MaxLength(50)]
        public string? UploadPlatform { get; set; }   // youtube / tiktok / instagram

        public AutoVideoAssetStatus Status { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Log { get; set; }    // JSON log
    }

}
