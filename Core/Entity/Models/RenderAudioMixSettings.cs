namespace Core.Entity.Models
{
    public class RenderAudioMixSettings
    {
        public int VoiceVolumePercent { get; set; } = 100; // %100
        public int MusicVolumePercent { get; set; } = 20;  // %20 (Kısık)
        public int SfxVolumePercent { get; set; } = 60;

        // Ducking: Ses konuşurken müzik kısılsın mı?
        public bool EnableDucking { get; set; } = true;
        public int DuckingFactor { get; set; } = 15; // Müziği %15'e çek

        // Fade In/Out
        public bool FadeAudioInOut { get; set; } = true;
        public double FadeDurationSec { get; set; } = 1.5;
    }
}
