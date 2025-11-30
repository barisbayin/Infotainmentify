namespace Application.Contracts.Presets
{

    public class RenderAudioMixSettingsDto
    {
        public int VoiceVolumePercent { get; set; } = 100;
        public int MusicVolumePercent { get; set; } = 20;
        public bool EnableDucking { get; set; } = true;
    }
}
