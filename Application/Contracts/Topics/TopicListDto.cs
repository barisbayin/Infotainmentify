namespace Application.Contracts.Topics
{
    public sealed class TopicListDto
    {
        public int Id { get; set; }

        // --- Kavramsal Bilgiler ---
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? Tone { get; set; }

        // --- Çekirdek Fikir ---
        public string? Premise { get; set; }
        public string? PremiseTr { get; set; }

        // --- Üretim Durumu ---

        public bool AllowScriptGeneration { get; set; }
        public bool ScriptGenerated { get; set; }    // script üretimi tamam mı
        public bool IsActive { get; set; }           // topic üretime açık mı
        public bool CanGenerate => !ScriptGenerated && IsActive; // hesaplanabilir alan (UI için)

        // --- Prompt Bilgisi ---
        public int? PromptId { get; set; }
        public string? PromptName { get; set; }

        // --- Meta ---
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
