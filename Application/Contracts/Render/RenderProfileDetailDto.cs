using Core.Enums;

namespace Application.Contracts.Render
{
    public class RenderProfileDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;

        // RESOLUTION & FPS
        public string Resolution { get; set; } = default!;
        public int Fps { get; set; }

        // STYLE
        public string? Style { get; set; }

        // CAPTIONS
        public string CaptionStyle { get; set; } = default!;
        public string CaptionFont { get; set; } = default!;
        public int CaptionSize { get; set; }
        public bool CaptionGlow { get; set; }
        public string CaptionGlowColor { get; set; } = default!;
        public int CaptionGlowSize { get; set; }
        public int CaptionOutlineSize { get; set; }
        public int CaptionShadowSize { get; set; }
        public bool CaptionKaraoke { get; set; }
        public string CaptionHighlightColor { get; set; } = default!;
        public int CaptionChunkSize { get; set; }
        public CaptionPositionTypes CaptionPosition { get; set; }
        public int CaptionMarginV { get; set; }
        public double CaptionLineSpacing { get; set; }
        public int CaptionMaxWidthPercent { get; set; }
        public double CaptionBackgroundOpacity { get; set; }
        public CaptionAnimationTypes CaptionAnimation { get; set; }


        // MOTION
        public double MotionIntensity { get; set; }
        public double ZoomSpeed { get; set; }
        public double ZoomMax { get; set; }
        public double PanX { get; set; }
        public double PanY { get; set; }

        // TRANSITIONS
        public string Transition { get; set; } = default!;
        public double TransitionDuration { get; set; }
        public string TransitionDirection { get; set; } = default!;
        public string TransitionEasing { get; set; } = default!;
        public double TransitionStrength { get; set; }

        // TIMELINE
        public string TimelineMode { get; set; } = default!;

        // AUDIO MIX
        public int BgmVolume { get; set; }
        public int VoiceVolume { get; set; }
        public int DuckingStrength { get; set; }

        // AI Metadata (readonly)
        public string? AiRecommendedStyle { get; set; }
        public string? AiRecommendedTransitions { get; set; }
        public string? AiRecommendedCaption { get; set; }
    }

}
