namespace Application.Contracts.Script
{
    public sealed class ScriptDetailDto
    {
        public int Id { get; set; }

        // 📄 İçerik
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string? Summary { get; set; }

        public string? Language { get; set; }
        public string? RenderStyle { get; set; }
        public string? ProductionType { get; set; }

        // 🔗 İlişki ID'leri
        public int TopicId { get; set; }
        public int? PromptId { get; set; }
        public int? AiConnectionId { get; set; }
        public int? ScriptGenerationProfileId { get; set; }

        // 🧩 İlişkili varlık isimleri (read-only)
        public string? TopicCode { get; set; }
        public string? TopicPremise { get; set; }
        public string? PromptName { get; set; }
        public string? AiProvider { get; set; }
        public string? ModelName { get; set; }
        public string? ProfileName { get; set; }

        // ⚙️ Meta ve JSON alanları
        public string? MetaJson { get; set; }
        public string? ScriptJson { get; set; }

        // ⏱️ Sistemsel
        public int? ResponseTimeMs { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
