namespace Core.Entity.Models
{
    public class RenderVisualEffectsSettings
    {
        // Ken Burns (Resimlere hareket katma)
        public bool EnableKenBurns { get; set; } = true;
        public double ZoomIntensity { get; set; } = 1.1; // %10 zoom yap

        // Geçişler
        public string TransitionType { get; set; } = "crossfade"; // fade, wipe, slide
        public double TransitionDurationSec { get; set; } = 0.5;

        // Renk Düzenleme (LUT)
        public string? ColorFilter { get; set; } // "cinematic_warm", "bw_noir"

        // Video Hızı (Speed Ramp) - İleride eklenebilir
        // public double SpeedMultiplier { get; set; } = 1.0; 
    }
}
