namespace Application.Models
{
    public class UploadResultItem
    {
        public string Platform { get; set; } = string.Empty;   // "YouTube", "Instagram"
        public string ChannelName { get; set; } = string.Empty; // "Gemini TR"

        public string? VideoUrl { get; set; }     // Başarılıysa Link

        public bool IsSuccess { get; set; }       // Başarılı mı?
        public string? ErrorMessage { get; set; } // Hata varsa ne?
    }
}
