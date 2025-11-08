namespace Application.Models
{
    public class TopicGenerationRequest
    {
        // 🧠 Prompts
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;

        // ⚙️ AI üretim ayarları
        public int Count { get; set; } = 5;
        public string? Model { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int? MaxTokens { get; set; }
        public string Language { get; set; } = "en";

        // 🎬 Üretim bağlamı
        public string? ProductionType { get; set; }   // örn: "shorts", "microdoc", "aiart"
        public string? RenderStyle { get; set; }      // örn: "realistic", "anime", "documentary"
        public string OutputMode { get; set; } = "Topic"; // "Topic", "Script", "Image" veya "Mixed"

        // 🔖 Sınıflandırma / içerik bilgisi
        public string? Category { get; set; }
        public string? SubCategory { get; set; }

        // 🏷️ Ek etiket bilgisi
        public string? TagsJson { get; set; } // JSON array: ["psychology","funny","viral"]

        // 🔗 Bağlantı bilgileri
        public int? ProfileId { get; set; }
        public int? UserId { get; set; }

        // ⚙️ AI spesifik parametreler (örneğin Gemini veya OpenAI özel ayarları)
        public Dictionary<string, string>? ExtraParameters { get; set; }
    }
}
