using Core.Enums;

namespace Application.Abstractions
{
    public interface ISocialUploaderFactory
    {
        ISocialUploader Resolve(SocialChannelType platform);
    }
}
