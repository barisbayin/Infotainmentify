using Core.Abstractions;
using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class VideoGenerationProfile : BaseEntity, IJobProfile
    {
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public AppUser User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ProfileName { get; set; } = null!;

        // Script üretim profili
        public int ScriptGenerationProfileId { get; set; }

        [ForeignKey(nameof(ScriptGenerationProfileId))]
        public ScriptGenerationProfile ScriptGenerationProfile { get; set; } = null!;

        // Upload behavior
        public int? SocialChannelId { get; set; }

        [ForeignKey(nameof(SocialChannelId))]
        public UserSocialChannel? SocialChannel { get; set; }

        public bool UploadAfterRender { get; set; } = true;
        public bool GenerateThumbnail { get; set; } = true;

        [MaxLength(200)]
        public string? TitleTemplate { get; set; }

        [MaxLength(2000)]
        public string? DescriptionTemplate { get; set; }

        public JobType JobType => throw new NotImplementedException();

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
