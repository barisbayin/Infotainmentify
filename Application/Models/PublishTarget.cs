namespace Application.Models
{
    public class PublishTarget
    {
        public int SocialChannelId { get; set; } // Hangi Kanal?

        // --- Platforma Özel Override Ayarları ---

        // Başlık Şablonu. Örn: "{Title} - #Shorts"
        // Eğer null bırakırsan AI'dan gelen ham başlığı kullanır.
        public string? TitleTemplate { get; set; }

        // Açıklama Şablonu. Örn: "{Description}\n\nFollow us for more!"
        public string? DescriptionTemplate { get; set; }

        // Sadece bu platforma özel ekstra etiketler. 
        // Örn: Instagram için ["#reels", "#explore"], YouTube için ["#shorts"]
        public List<string> PlatformTags { get; set; } = new();

        public string PrivacyStatus { get; set; } = "private";
    }
}
