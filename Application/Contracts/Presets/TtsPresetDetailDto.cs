namespace Application.Contracts.Presets
{
    // DETAIL
    public class TtsPresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int UserAiConnectionId { get; set; }

        public string VoiceId { get; set; } = default!;
        public string LanguageCode { get; set; } = default!;
        public string? EngineModel { get; set; } // "eleven_multilingual_v2"

        public double SpeakingRate { get; set; }
        public double Pitch { get; set; }
        public double Stability { get; set; }
        public double Clarity { get; set; }
        public double StyleExaggeration { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
