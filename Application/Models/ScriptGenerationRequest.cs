using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    /// <summary>
    /// Script üretim isteği modeli.
    /// Belirli topic(ler) için AI destekli senaryo / metin üretimi yapılır.
    /// </summary>
    public class ScriptGenerationRequest
    {
        // 🧠 Prompts
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;

        // ⚙️ AI üretim ayarları
        public string? Model { get; set; }
        public double Temperature { get; set; } = 0.8;
        public int? MaxTokens { get; set; }
        public string Language { get; set; } = "en";

        // 🎬 Üretim bağlamı
        public string OutputMode { get; set; } = "Script";  // "Script", "Story", "Dialogue"
        public string? ProductionType { get; set; }         // "shorts", "microdoc", "aiart"
        public string? RenderStyle { get; set; }            // "cinematic_vertical", "fastcut_info"

        // 🔗 Bağlantı bilgileri
        public int? ProfileId { get; set; }
        public int? TopicId { get; set; }
        public int? UserId { get; set; }

        // 🧩 Üretim parametreleri
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Premise { get; set; }     // Topic.Premise
        public string? Tone { get; set; }        // Topic.Tone
        public string? PotentialVisual { get; set; }

        // ⚙️ AI spesifik parametreler
        public Dictionary<string, string>? ExtraParameters { get; set; }

        // 🔖 İlişkili Config (profil ayarları)
        public string? ConfigJson { get; set; }
    }
}
