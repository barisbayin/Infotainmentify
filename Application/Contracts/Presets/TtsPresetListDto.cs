namespace Application.Contracts.Presets
{
    // LIST
    public class TtsPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string VoiceId { get; set; } = default!;
        public string LanguageCode { get; set; } = default!;
        public DateTime? UpdatedAt { get; set; }
    }
}
