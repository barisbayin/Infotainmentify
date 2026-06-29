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

        // YouTube-oriented mastering. Voice is normalized before music/SFX are mixed.
        public bool EnableVoiceLoudnessNormalization { get; set; } = true;
        public double VoiceLoudnessTargetI { get; set; } = -16.0;
        public double VoiceLoudnessTargetTp { get; set; } = -1.5;
        public double VoiceLoudnessRange { get; set; } = 11.0;

        // Softer ducking controls for long-form narration.
        public double DuckingThreshold { get; set; } = 0.05;
        public double DuckingRatio { get; set; } = 5.0;
        public int DuckingAttackMs { get; set; } = 80;
        public int DuckingReleaseMs { get; set; } = 420;

        public int FinalAudioBitrateKbps { get; set; } = 192;
        public bool EnableFinalAudioQa { get; set; } = true;

        // Editorial audio timing: lets SceneLayout add subtle J-cut/L-cut style voice offsets.
        public bool EnableEditorAudioCuts { get; set; } = true;
        public double MaxEditorAudioOffsetSec { get; set; } = 0.22;
        public double VoiceMicroFadeSec { get; set; } = 0.04;
    }
}
