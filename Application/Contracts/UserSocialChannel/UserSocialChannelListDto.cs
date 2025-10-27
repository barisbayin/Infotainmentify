namespace Application.Contracts.UserSocialChannel
{
    public class UserSocialChannelListDto
    {
        public int Id { get; set; }

        // Enum'ın string karşılığını (YouTube, Instagram) döndürmek 
        // UI (Arayüz) için daha kullanışlıdır.
        public string ChannelType { get; set; }

        public string ChannelName { get; set; }

        // Kanalın @kullaniciadi gibi bilgisi
        public string ChannelHandle { get; set; }

        public bool IsActive { get; set; }

        public DateTime LastVerifiedAt { get; set; }

        /* * Servis katmanında veya AutoMapper profilinde hesaplanacak ekstra bir özellik.
         * UI'da "Bağlantı Süresi Doldu" gibi bir uyarı göstermek için kullanışlıdır.
         * Hesaplama: (TokenExpiresAt.HasValue && TokenExpiresAt.Value <= DateTime.UtcNow) ? false : true
         */
        public bool IsConnectionValid { get; set; }
    }
}
