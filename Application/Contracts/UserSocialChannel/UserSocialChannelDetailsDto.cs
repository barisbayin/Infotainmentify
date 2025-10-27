using Application.Contracts.Enums;

namespace Application.Contracts.UserSocialChannel
{
    /* * Tek bir sosyal kanalın ayrıntılarını göstermek için kullanılır.
     * API anahtarları gibi hassas bilgiler KESİNLİKLE DAHİL EDİLMEZ.
     */
    public class UserSocialChannelDetailsDto
    {
        // Önceki tüm alanlar (Id, ChannelType, ChannelName, Scopes vb.)
        public int Id { get; set; }
        public string ChannelType { get; set; }
        public string ChannelName { get; set; }
        // ...diğer alanlar...
        public DateTime? TokenExpiresAt { get; set; }

        // DİKKAT: Bu alanlar artık JSON'dan DOLDURULACAK (DB'de yok)
        // Create/Update için PLAIN, Get için MASKED veya PLAIN olabilirler
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        // GetAsync'in ne döndürdüğünü belirtmek için (UserAiConnection'daki gibi)
        public CredentialExposure ExposureLevel { get; set; } = CredentialExposure.None;
    }
}
