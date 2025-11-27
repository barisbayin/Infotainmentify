using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class AutoVideoRenderProfile : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }

        [Required, MaxLength(64)]
        public string Name { get; set; } = default!;

        // -------------------------------------------------
        // RESOLUTION & FPS
        // -------------------------------------------------
        [MaxLength(16)]
        public string Resolution { get; set; } = "1080x1920";

        public int Fps { get; set; } = 30;

        // -------------------------------------------------
        // GENERAL STYLE
        // -------------------------------------------------
        [MaxLength(64)]
        public string? Style { get; set; }  // "viral_glow", "neon_hyper"

        // -------------------------------------------------
        // CAPTION SYSTEM (PREMIUM SUBTITLE ENGINE)
        // -------------------------------------------------
        [MaxLength(64)]
        public string CaptionStyle { get; set; } = "mrbeast_glow";

        [MaxLength(64)]
        public string CaptionFont { get; set; } = "Arial";

        public int CaptionSize { get; set; } = 48;

        public bool CaptionGlow { get; set; } = true;

        [MaxLength(16)]
        public string CaptionGlowColor { get; set; } = "#00FFFF"; // cyan glow

        public int CaptionGlowSize { get; set; } = 12;

        public int CaptionOutlineSize { get; set; } = 2;

        public int CaptionShadowSize { get; set; } = 4;

        public bool CaptionKaraoke { get; set; } = false;

        [MaxLength(16)]
        public string CaptionHighlightColor { get; set; } = "#FFD700"; // gold

        // ek: kelime chunk sistemi
        public int CaptionChunkSize { get; set; } = 2; // "2 kelime 2 kelime"

        [Required]
        public CaptionPositionTypes CaptionPosition { get; set; } = CaptionPositionTypes.Top;

        public int CaptionMarginV { get; set; } = 90;

        public double CaptionLineSpacing { get; set; } = 1.15;

        public int CaptionMaxWidthPercent { get; set; } = 70;

        public double CaptionBackgroundOpacity { get; set; } = 0.0;

        public CaptionAnimationTypes CaptionAnimation { get; set; } = CaptionAnimationTypes.None;


        // -------------------------------------------------
        // MOTION (KEN BURNS)
        // -------------------------------------------------
        public double MotionIntensity { get; set; } = 1.0; // 1=default, 2=more dramatic
        public double ZoomSpeed { get; set; } = 0.00025;
        public double ZoomMax { get; set; } = 1.08;
        public double PanX { get; set; } = 0.0;
        public double PanY { get; set; } = 0.0;

        // -------------------------------------------------
        // TRANSITIONS
        // -------------------------------------------------
        [MaxLength(32)]
        public string Transition { get; set; } = "crossfade";

        public double TransitionDuration { get; set; } = 0.35;

        [MaxLength(16)]
        public string TransitionDirection { get; set; } = "up";

        [MaxLength(16)]
        public string TransitionEasing { get; set; } = "linear";

        public double TransitionStrength { get; set; } = 1.0;

        // -------------------------------------------------
        // TIMELINE MODE
        // -------------------------------------------------
        [MaxLength(32)]
        public string TimelineMode { get; set; } = "even";

        // -------------------------------------------------
        // AUDIO MIX
        // -------------------------------------------------
        public int BgmVolume { get; set; } = 40;
        public int VoiceVolume { get; set; } = 100;
        public int DuckingStrength { get; set; } = 30;

        // -------------------------------------------------
        // AI RECOMMENDATIONS
        // -------------------------------------------------
        [MaxLength(64)]
        public string? AiRecommendedStyle { get; set; }

        [MaxLength(64)]
        public string? AiRecommendedTransitions { get; set; }

        [MaxLength(64)]
        public string? AiRecommendedCaption { get; set; }
    }
}
