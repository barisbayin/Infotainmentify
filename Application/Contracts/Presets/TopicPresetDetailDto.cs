namespace Application.Contracts.Presets
{
    // =================================================================
    // 2. DETAIL DTO (Detaylı - Form Doldurmak İçin)
    // =================================================================
    public class TopicPresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public int UserAiConnectionId { get; set; }

        public string ModelName { get; set; } = default!;
        public float Temperature { get; set; }
        public string Language { get; set; } = default!;

        // Ağır Veriler (Blob/Text)
        public string PromptTemplate { get; set; } = default!;
        public string? ContextKeywordsJson { get; set; }
        public string? SystemInstruction { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
