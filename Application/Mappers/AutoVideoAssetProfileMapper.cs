using Application.Contracts.AutoVideoAsset;
using Core.Entity;

namespace Application.Mappers
{
    public static class AutoVideoAssetProfileMapper
    {
        public static AutoVideoAssetProfileListDto ToListDto(this AutoVideoAssetProfile x)
        {
            return new AutoVideoAssetProfileListDto
            {
                Id = x.Id,
                ProfileName = x.ProfileName,
                TopicProfileName = x.TopicGenerationProfile.ProfileName,
                ScriptProfileName = x.ScriptGenerationProfile.ProfileName,
                SocialChannelName = x.SocialChannel?.ChannelName,
                UploadAfterRender = x.UploadAfterRender,
                GenerateThumbnail = x.GenerateThumbnail,
                IsActive = x.IsActive
            };
        }

        public static AutoVideoAssetProfileDetailDto ToDetailDto(this AutoVideoAssetProfile x)
        {
            return new AutoVideoAssetProfileDetailDto
            {
                Id = x.Id,
                ProfileName = x.ProfileName,
                TopicGenerationProfileId = x.TopicGenerationProfileId,
                ScriptGenerationProfileId = x.ScriptGenerationProfileId,
                SocialChannelId = x.SocialChannelId,
                UploadAfterRender = x.UploadAfterRender,
                GenerateThumbnail = x.GenerateThumbnail,
                TitleTemplate = x.TitleTemplate,
                DescriptionTemplate = x.DescriptionTemplate,
                IsActive = x.IsActive
            };
        }
    }
}
