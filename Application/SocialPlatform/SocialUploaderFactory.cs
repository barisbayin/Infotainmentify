using Application.Abstractions;
using Core.Enums;

namespace Application.SocialPlatform
{
    public class SocialUploaderFactory : ISocialUploaderFactory
    {
        private readonly YouTubeUploader _youtube;

        public SocialUploaderFactory(YouTubeUploader youtube)
        {
            _youtube = youtube;
        }

        public ISocialUploader Resolve(SocialChannelType platform)
        {
            return platform switch
            {
                SocialChannelType.YouTube => _youtube,
                _ => throw new NotSupportedException($"Platform desteklenmiyor: {platform}")
            };
        }
    }
}
