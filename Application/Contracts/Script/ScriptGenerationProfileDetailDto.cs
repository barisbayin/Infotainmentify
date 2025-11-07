namespace Application.Contracts.Script
{
    public class ScriptGenerationProfileDetailDto
    {
        public int Id { get; set; }

        public string ProfileName { get; set; } = default!;
        public int PromptId { get; set; }
        public int AiConnectionId { get; set; }
        public int? TopicGenerationProfileId { get; set; }

        public string ModelName { get; set; } = default!;
        public double Temperature { get; set; } = 0.8;
        public string? Language { get; set; }

        public string? TopicIdsJson { get; set; }
        public string? ConfigJson { get; set; }
        public string? RawResponseJson { get; set; }

        public string? Status { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        // Görsel alanlar (liste veya detayda gösterim için)
        public string? PromptName { get; set; }
        public string? AiConnectionName { get; set; }
        public string? AiProvider { get; set; }
        public string? TopicGenerationProfileName { get; set; }
    }
}
