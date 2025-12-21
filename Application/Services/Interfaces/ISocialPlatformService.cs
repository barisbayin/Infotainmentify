using Application.Models;
using Core.Entity;
using Core.Enums;

namespace Application.Services.Interfaces
{
    public interface ISocialPlatformService
    {
        // Her platformun "ChannelType"ı vardır (Enum)
        SocialChannelType Type { get; }

        // Videoyu yükler ve URL döner
        Task<string> UploadAsync(
            SocialChannel channel, // Tokenlar burada
            string videoPath,
            SocialMetadata metadata, // Başlık, açıklama paketi
            CancellationToken ct = default
        );
    }
}
