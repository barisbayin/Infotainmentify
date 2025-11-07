namespace Application.Models
{
    /// <summary>
    /// Topic üretimi için yapılandırılmış AI çağrı parametreleri.
    /// </summary>
    public class TopicGenerationRequest
    {
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public int Count { get; set; } = 5;
        public string? Model { get; set; }
        public double Temperature { get; set; } = 0.7;

        // 🎬 Üretim bağlamı
        public string? ProductionType { get; set; }   // örn: "shorts", "microdoc", "aiart"
        public string? RenderStyle { get; set; }      // örn: "realistic", "anime", "documentary"

        // 🔖 Sınıflandırma / içerik bilgisi
        public string? Category { get; set; }
        public string? SubCategory { get; set; }

        // 🔗 Bağlantı bilgileri
        public int? ProfileId { get; set; }
        public int? UserId { get; set; }

        // ⚙️ AI spesifik parametreler (örneğin Gemini veya OpenAI özel ayarları)
        public Dictionary<string, string>? ExtraParameters { get; set; }
    }
}
