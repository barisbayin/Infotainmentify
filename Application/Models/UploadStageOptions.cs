namespace Application.Models
{
    public class UploadStageOptions
    {
        // Genel varsayılan ayarlar (Fallback)
        public string DefaultPrivacy { get; set; } = "private";

        // 🔥 BURASI DEĞİŞTİ: Artık detaylı hedef listesi tutuyoruz
        public List<PublishTarget> Targets { get; set; } = new();
    }
}
