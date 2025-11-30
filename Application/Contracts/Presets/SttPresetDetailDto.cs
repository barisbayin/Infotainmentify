namespace Application.Contracts.Presets
{
    // DETAIL
    public class SttPresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int UserAiConnectionId { get; set; }

        public string ModelName { get; set; } = default!;
        public string LanguageCode { get; set; } = default!;

        public bool EnableWordLevelTimestamps { get; set; } // Karaoke için şart
        public bool EnableSpeakerDiarization { get; set; }
        public string OutputFormat { get; set; } = "json";

        public string? Prompt { get; set; } // Whisper'a ipucu (Özel isimler vb.)
        public double Temperature { get; set; }
        public bool FilterProfanity { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
