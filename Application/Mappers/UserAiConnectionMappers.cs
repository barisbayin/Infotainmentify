using Application.Contracts.AppUser;
using Core.Entity.User;

namespace Application.Mappers
{
    public static class UserAiConnectionMapper
    {
        // -----------------------------------------------------------
        // Entity -> List DTO (Grid için)
        // -----------------------------------------------------------
        public static UserAiConnectionListDto ToListDto(this UserAiConnection e)
        {
            return new UserAiConnectionListDto
            {
                Id = e.Id,
                Name = e.Name,
                // Enum'ı string olarak dönüyoruz ki FE'de "OpenAI" diye okunsun
                Provider = e.Provider.ToString(),
                CreatedAt = e.CreatedAt
            };
        }

        // -----------------------------------------------------------
        // Entity -> Detail DTO (Detay/Edit için)
        // -----------------------------------------------------------
        public static UserAiConnectionDetailDto ToDetailDto(this UserAiConnection e)
        {
            return new UserAiConnectionDetailDto
            {
                Id = e.Id,
                Name = e.Name,
                Provider = e.Provider.ToString(),
                ExtraId = e.ExtraId,

                // 🔒 GÜVENLİK HAMLESİ:
                // Veritabanındaki şifreli datayı (EncryptedApiKey) ASLA dışarı dönmüyoruz.
                // Kullanıcı sadece orada bir key olduğunu bilsin yeter.
                MaskedApiKey = e.EncryptedApiKey //"****************"
            };
        }
    }
}
