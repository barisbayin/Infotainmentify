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

        // Long-form icin sahne ici gorsel ritim. Gercek ek B-roll gorselleri gelene kadar
        // uzun sahneleri farkli pan/zoom hareketlerine bolerek tek gorsel monotonlugunu azaltir.
        public bool EnableAutoBroll { get; set; } = false;
        public int MinSceneDurationForBrollSec { get; set; } = 18;
        public int BrollSegmentDurationSec { get; set; } = 10;
        public int MaxBrollCutsPerScene { get; set; } = 5;

        // Edit plan/storyboard tarafindan gelen kisa vurgu metinlerini final render uzerine basar.
        // Long-form icin varsayilan kapali; gorselin icindeki metni creative director/image prompt belirlesin.
        public bool EnableOverlayText { get; set; } = false;

        // Video Hızı (Speed Ramp) - İleride eklenebilir
        // public double SpeedMultiplier { get; set; } = 1.0; 
    }
}
