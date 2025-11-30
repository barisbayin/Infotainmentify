using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Presets
{
    // SAVE
    public class SaveSttPresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public int UserAiConnectionId { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "whisper-1";

        [MaxLength(10)]
        public string LanguageCode { get; set; } = "auto";

        public bool EnableWordLevelTimestamps { get; set; } = true;
        public bool EnableSpeakerDiarization { get; set; } = false;

        [MaxLength(10)]
        public string OutputFormat { get; set; } = "json";

        [MaxLength(1000)]
        public string? Prompt { get; set; }

        [Range(0.0, 1.0)]
        public double Temperature { get; set; } = 0.0;

        public bool FilterProfanity { get; set; } = false;
    }
}
