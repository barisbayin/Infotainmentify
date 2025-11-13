using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class AutoVideoAssetProfile : BaseEntity
    {
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ProfileName { get; set; } = null!;

        // 1) Topic üretim profili
        [Required]
        public int TopicGenerationProfileId { get; set; }

        [ForeignKey(nameof(TopicGenerationProfileId))]
        public TopicGenerationProfile TopicGenerationProfile { get; set; } = null!;

        // 2) Script + Asset üretim profili
        [Required]
        public int ScriptGenerationProfileId { get; set; }

        [ForeignKey(nameof(ScriptGenerationProfileId))]
        public ScriptGenerationProfile ScriptGenerationProfile { get; set; } = null!;

        // 3) Upload behavior
        public int? SocialChannelId { get; set; }

        [ForeignKey(nameof(SocialChannelId))]
        public UserSocialChannel? SocialChannel { get; set; }

        public bool UploadAfterRender { get; set; } = true;
        public bool GenerateThumbnail { get; set; } = true;

        [MaxLength(200)]
        public string? TitleTemplate { get; set; }

        [MaxLength(2000)]
        public string? DescriptionTemplate { get; set; }
    }

}
