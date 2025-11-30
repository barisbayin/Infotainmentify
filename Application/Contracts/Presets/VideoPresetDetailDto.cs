using Core.Enums;

namespace Application.Contracts.Presets
{
    // DETAIL & SAVE
    // Not: Camera ve Advanced ayarlarını JSON string olarak taşıyoruz (Esneklik için)
    public class VideoPresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int UserAiConnectionId { get; set; }

        public string ModelName { get; set; } = default!;
        public VideoGenerationMode GenerationMode { get; set; }

        public string AspectRatio { get; set; } = default!;
        public int DurationSeconds { get; set; }

        public string PromptTemplate { get; set; } = default!;
        public string? NegativePrompt { get; set; }

        public string? CameraControlSettingsJson { get; set; }
        public string? AdvancedSettingsJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
