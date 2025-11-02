namespace Application.Contracts.TopicGenerationProfile
{
    public sealed class TopicGenerationProfileDetailDto
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = default!;
        public int PromptId { get; set; }
        public int AiConnectionId { get; set; }
        public string ModelName { get; set; } = default!;
        public int RequestedCount { get; set; }
        public string? RawResponseJson { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? Status { get; set; }

        // İlişkili isimler (detay ekranında göstermek için)
        public string? PromptName { get; set; }
        public string? AiProvider { get; set; }
    }
}
