namespace Application.Models
{
    /// <summary>
    /// AI modellerinden dönen Topic üretim sonucu (tek öğe)
    /// TopicGenerationService -> Topic entity'ye dönüştürülürken kullanılır.
    /// </summary>
    public class TopicResult
    {
        public string Id { get; set; } = default!;

        // ---- Kavramsal alanlar ----
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Series { get; set; }

        // ---- Ana fikir ----
        public string Premise { get; set; } = default!;
        public string? PremiseTr { get; set; }

        // ---- Üslup ----
        public string? Tone { get; set; }

        // ---- Görsel ipucu ----
        public string? PotentialVisual { get; set; }

        // ---- Render tarzı ----
        public string? RenderStyle { get; set; }

        // ---- Seslendirme / Yazım ipuçları ----
        public string? VoiceHint { get; set; }
        public string? ScriptHint { get; set; }

        // ---- AI meta bilgileri ----
        public object? Metadata { get; set; }

        // ---- Bayraklar ----
        public bool NeedsFootage { get; set; } = false;
        public bool FactCheck { get; set; } = false;

        // ---- Öncelik puanı (1–10 arası) ----
        public int Priority { get; set; } = 5;

        // ---- Etiketler ----
        public List<string> Tags { get; set; } = new();
    }
}
