using Application.Contracts.AutoVideoAsset;
using Core.Entity;

namespace Application.Mappers
{
    public static class VideoGenerationProfileMapper
    {
        public static VideoGenerationProfileListDto ToListDto(this VideoGenerationProfile e)
        {
            return new VideoGenerationProfileListDto
            {
                Id = e.Id,
                ProfileName = e.ProfileName,
                IsActive = e.IsActive,

                ScriptGenerationProfileId = e.ScriptGenerationProfileId,
                ScriptGenerationProfileName = e.ScriptGenerationProfile?.ProfileName ?? "",

                SocialChannelId = e.SocialChannelId,
                SocialChannelName = e.SocialChannel?.ChannelName
            };
        }

        public static VideoGenerationProfileDetailDto ToDetailDto(this VideoGenerationProfile e)
        {
            return new VideoGenerationProfileDetailDto
            {
                Id = e.Id,
                ProfileName = e.ProfileName,
                ScriptGenerationProfileId = e.ScriptGenerationProfileId,
                SocialChannelId = e.SocialChannelId,

                UploadAfterRender = e.UploadAfterRender,
                GenerateThumbnail = e.GenerateThumbnail,
                TitleTemplate = e.TitleTemplate,
                DescriptionTemplate = e.DescriptionTemplate,

                IsActive = e.IsActive
            };
        }

        public static void Apply(this VideoGenerationProfile e, VideoGenerationProfileDetailDto dto)
        {
            e.ProfileName = dto.ProfileName.Trim();
            e.ScriptGenerationProfileId = dto.ScriptGenerationProfileId;
            e.SocialChannelId = dto.SocialChannelId;

            e.UploadAfterRender = dto.UploadAfterRender;
            e.GenerateThumbnail = dto.GenerateThumbnail;
            e.TitleTemplate = dto.TitleTemplate;
            e.DescriptionTemplate = dto.DescriptionTemplate;

            e.IsActive = dto.IsActive;
        }
    }
}
