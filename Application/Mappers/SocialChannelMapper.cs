using Application.Contracts.UserSocialChannel;
using Core.Entity;
using System.Text.Json;

namespace Application.Mappers
{
    public static class SocialChannelMapper
    {
        // -----------------------------------------------------------
        // Entity -> List DTO (Grid için özet)
        // -----------------------------------------------------------
        public static SocialChannelListDto ToListDto(this SocialChannel e)
        {
            return new SocialChannelListDto
            {
                Id = e.Id,
                ChannelName = e.ChannelName ?? "Unknown",
                // Enum'ı string'e çeviriyoruz ("YouTube", "Instagram" vs.)
                Platform = e.ChannelType.ToString(),
                ChannelUrl = e.ChannelUrl,
                CreatedAt = e.CreatedAt
            };
        }

        // -----------------------------------------------------------
        // Entity -> Detail DTO (Detay sayfası için)
        // -----------------------------------------------------------
        public static SocialChannelDetailDto ToDetailDto(this SocialChannel e)
        {
            return new SocialChannelDetailDto
            {
                Id = e.Id,
                ChannelName = e.ChannelName ?? "Unknown",
                Platform = e.ChannelType.ToString(),
                ChannelHandle = e.ChannelHandle,
                ChannelUrl = e.ChannelUrl,
                PlatformChannelId = e.PlatformChannelId,

                TokenExpiresAt = e.TokenExpiresAt,

                // 🧠 Akıllı Mapping: Token süresi dolmuş mu?
                // Frontend buna bakıp "Yeniden Giriş Yap" butonu çıkarabilir.
                IsTokenExpired = e.TokenExpiresAt.HasValue && e.TokenExpiresAt < DateTimeOffset.UtcNow,

                Scopes = e.Scopes,

                EncryptedTokensJson = e.EncryptedTokensJson

                // DİKKAT: Tokens/EncryptedTokensJson alanlarını buraya koymuyoruz.
                // Güvenlik gereği dışarı çıkmamalı.
            };
        }
    }

}
