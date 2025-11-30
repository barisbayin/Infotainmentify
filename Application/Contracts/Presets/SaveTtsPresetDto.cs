using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Presets
{
    // SAVE
    public class SaveTtsPresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public int UserAiConnectionId { get; set; }

        [Required, MaxLength(100)]
        public string VoiceId { get; set; } = default!;

        [Required, MaxLength(20)]
        public string LanguageCode { get; set; } = "tr-TR";

        [MaxLength(100)]
        public string? EngineModel { get; set; }

        // Google için 0.25 - 4.0 arası (1.0 normal)
        public double SpeakingRate { get; set; } = 1.0;

        // Google için -20.0 ile 20.0 arası (0 normal)
        public double Pitch { get; set; } = 0.0;

        // ElevenLabs (0.0 - 1.0)
        public double Stability { get; set; } = 0.5;
        public double Clarity { get; set; } = 0.75;
        public double StyleExaggeration { get; set; } = 0.0;
    }
}
