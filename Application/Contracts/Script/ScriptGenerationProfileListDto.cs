namespace Application.Contracts.Script
{
    public class ScriptGenerationProfileListDto
    {
        public int Id { get; set; }

        // 🏷️ Genel bilgiler
        public string ProfileName { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string Language { get; set; } = default!;
        public string OutputMode { get; set; } = default!;
        public string? ProductionType { get; set; }
        public string? RenderStyle { get; set; }
        public float Temperature { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowRetry { get; set; }
        public string Status { get; set; } = default!;

        // 🔗 İlişkisel bilgiler
        public int AiConnectionId { get; set; }
        public string AiConnectionName { get; set; } = default!;
        public string AiProvider { get; set; } = default!; // örn: "OpenAI", "Gemini"

        public int PromptId { get; set; }
        public string PromptName { get; set; } = default!;

        public int? TopicGenerationProfileId { get; set; }
        public string? TopicGenerationProfileName { get; set; }
    }
}
