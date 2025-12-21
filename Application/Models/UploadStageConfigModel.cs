namespace Application.Models
{
    public class UploadStageConfigModel
    {
        // Hangi kanala gidecek? (Senin SocialChannel tablundaki ID)
        public int TargetChannelId { get; set; }

        // --- Metadata Ayarları ---

        // Eğer null ise Script'ten gelen Title'ı kullan. 
        // Doluysa bunu kullan (Override).
        public string? TitleTemplate { get; set; }
        // Örn: "{GeneratedTitle} - #Shorts" gibi format da destekleyebiliriz.

        public string? DescriptionTemplate { get; set; }

        public string PrivacyStatus { get; set; } = "private"; // public, unlisted, private

        public bool PublishToShorts { get; set; } = true; // YouTube için özel flag

        // Sabit etiketler (AI'ın ürettiklerine ek olarak)
        public List<string> FixedTags { get; set; } = new();

        // Thumbnail seçimi (Otomatik, Frame 0, veya Custom Path)
        public string ThumbnailStrategy { get; set; } = "Auto";
    }
}
