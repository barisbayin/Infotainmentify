using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class AutoVideoPipeline : BaseEntity
    {
        // ---- Kullanıcı ----
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        // ---- Kullanılan Profil ----
        public int ProfileId { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public VideoGenerationProfile Profile { get; set; } = null!;

        // ---- Topic ----
        public int? TopicId { get; set; }

        [ForeignKey(nameof(TopicId))]
        public Topic? Topic { get; set; }

        // ---- Script ----
        public int? ScriptId { get; set; }

        [ForeignKey(nameof(ScriptId))]
        public Script? Script { get; set; }

        // ---- Üretilen Asset Bilgileri ----
        public string? ImagePathsJson { get; set; }     // JSON array
        public string? AudioPathsJson { get; set; }     // JSON array

        // ---- Render Bilgisi ----
        [MaxLength(500)]
        public string? VideoPath { get; set; }

        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }

        // ---- Final Meta Bilgileri ----
        [MaxLength(300)]
        public string? FinalTitle { get; set; }

        [MaxLength(2000)]
        public string? FinalDescription { get; set; }

        // ---- Upload Bilgileri ----
        public int? SocialChannelId { get; set; }

        [ForeignKey(nameof(SocialChannelId))]
        public UserSocialChannel? SocialChannel { get; set; }

        [MaxLength(200)]
        public string? UploadedVideoId { get; set; }

        [MaxLength(50)]
        public string? UploadedPlatform { get; set; }

        public DateTime? UploadedAt { get; set; }

        public bool Uploaded { get; set; } = false;

        // ---- Log ve Durum ----
        public string? LogJson { get; set; }        // JSON array (string[])
        public AutoVideoPipelineStatus Status { get; set; } = AutoVideoPipelineStatus.Pending;

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
