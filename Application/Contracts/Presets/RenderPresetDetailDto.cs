namespace Application.Contracts.Presets
{
    // DETAIL & SAVE (İç içe objelerle)
    public class RenderPresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int AppUserId { get; set; }

        // Video Core
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public int Fps { get; set; }
        public int BitrateKbps { get; set; }
        public string ContainerFormat { get; set; } = "mp4";

        // Nested Settings (Entity'deki NotMapped property'leri kullanacağız)
        public RenderCaptionSettingsDto CaptionSettings { get; set; } = new();
        public RenderAudioMixSettingsDto AudioMixSettings { get; set; } = new();

        // İleride eklenebilir: VisualEffectsSettings

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
