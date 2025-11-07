namespace Application.Contracts.Topics
{
    public sealed class TopicDetailDto
    {
        public int Id { get; set; }
        public string TopicCode { get; set; }

        // ---- Kavramsal Bilgiler ----
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Series { get; set; }

        // ---- İçerik Fikri ----
        public string? Premise { get; set; }
        public string? PremiseTr { get; set; }
        public string? Tone { get; set; }
        public string? PotentialVisual { get; set; }

        // ---- AI & Teknik Bilgiler ----
        public string? RenderStyle { get; set; }    // e.g. "bright_doc", "cinematic_dark"
        public string? VoiceHint { get; set; }      // e.g. "calm narrator"
        public string? ScriptHint { get; set; }     // e.g. "explanatory", "storytelling"
        public bool FactCheck { get; set; }
        public bool NeedsFootage { get; set; }
        public int Priority { get; set; }

        // ---- JSON Alanı ----
        public string? TopicJson { get; set; }

        // ---- Üretim Durumu ----
        public bool ScriptGenerated { get; set; }
        public DateTimeOffset? ScriptGeneratedAt { get; set; }

        // ---- Bağlantılar ----
        public int? PromptId { get; set; }
        public string? PromptName { get; set; }

        public int? ScriptId { get; set; }
        public string? ScriptTitle { get; set; }

        // ---- Durum ----
        public bool IsActive { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

}
