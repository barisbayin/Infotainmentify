using Application.Contracts.UserSocialChannel;
using Core.Entity;

namespace Application.Mappers
{
    public static class UserSocialChannelMappers
    {
        /**
         * Entity'yi Liste DTO'suna çevirir. (GÜVENLİ - Token içermez)
         */
        public static UserSocialChannelListDto ToListDto(this UserSocialChannel sc) => new()
        {
            Id = sc.Id,
            ChannelType = sc.ChannelType.ToString(),
            ChannelName = sc.ChannelName,
            ChannelHandle = sc.ChannelHandle,
            LastVerifiedAt = sc.LastVerifiedAt,
            IsConnectionValid = !(sc.TokenExpiresAt.HasValue && sc.TokenExpiresAt.Value <= DateTime.UtcNow)
        };

        /**
         * ***************************************************************
         * ! ! ! GÜVENLİK UYARISI ! ! !
         * Bu metod, HASSAS TOKEN'LARI DTO'ya MAP'LER.
         * Bu metodun döndürdüğü DTO'yu BİR API'DAN DIŞARI DÖNME!
         * ***************************************************************
         */
        public static UserSocialChannelDetailsDto ToDetailDto(this UserSocialChannel sc) => new()
        {
            Id = sc.Id,
            AppUserId = sc.AppUserId,
            ChannelType = sc.ChannelType.ToString(),
            ChannelName = sc.ChannelName,
            ChannelHandle = sc.ChannelHandle,
            ChannelUrl = sc.ChannelUrl,
            PlatformChannelId = sc.PlatformChannelId,
            Scopes = sc.Scopes,
            LastVerifiedAt = sc.LastVerifiedAt,
            TokenExpiresAt = sc.TokenExpiresAt,

            // --- RİSKLİ MAPPING ---
            // Bu token'lar asla frontend'e (tarayıcı/mobil) gitmemeli.
            AccessToken = sc.AccessToken,
            RefreshToken = sc.RefreshToken
            // -------------------------
        };

        /**
         * (Bonus) Liste DTO'su için IEnumerable (koleksiyon) mapper'ı.
         */
        public static IEnumerable<UserSocialChannelListDto> ToListDto(this IEnumerable<UserSocialChannel> channels)
        {
            return channels?.Select(c => c.ToListDto()) ?? Enumerable.Empty<UserSocialChannelListDto>();
        }
    }
}
