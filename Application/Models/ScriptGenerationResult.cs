namespace Application.Models
{
    /// <summary>
    /// Script üretim sürecinin sonuçlarını taşır.
    /// </summary>
    public class ScriptGenerationResult
    {
        /// <summary>Toplam işlenen topic sayısı.</summary>
        public int TotalRequested { get; set; }

        /// <summary>Başarıyla script üretilen topic sayısı.</summary>
        public int SuccessCount { get; set; }

        /// <summary>Üretim sırasında hata alınan topic sayısı.</summary>
        public int FailedCount { get; set; }

        /// <summary>Başarılı topic ID listesi.</summary>
        public List<int> GeneratedTopicIds { get; set; } = new();

        /// <summary>Hata alınan topic ID listesi.</summary>
        public List<int> FailedTopicIds { get; set; } = new();

        /// <summary>Kullanılan AI sağlayıcısı (örn: "GoogleVertex", "OpenAI").</summary>
        public string? Provider { get; set; }

        /// <summary>Kullanılan model adı (örn: "gemini-2.5-flash", "gpt-4-turbo").</summary>
        public string? Model { get; set; }

        /// <summary>Kullanılan sıcaklık (temperature) değeri.</summary>
        public double Temperature { get; set; }

        /// <summary>Üretim dili (örn: "en", "tr").</summary>
        public string? Language { get; set; }

        /// <summary>Üretim tipi (örn: "video", "image", "script").</summary>
        public string? ProductionType { get; set; }

        /// <summary>Render stili (örn: "cinematic_vertical", "handdrawn").</summary>
        public string? RenderStyle { get; set; }

        /// <summary>İsteğe bağlı açıklama veya özet metin.</summary>
        public string? Message { get; set; }

        public override string ToString()
        {
            return $"Toplam {TotalRequested} topic, {SuccessCount} başarı, {FailedCount} hata.";
        }
    }
}
