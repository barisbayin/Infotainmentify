using Application.Contracts.Render;
using Core.Entity;

namespace Application.Mappers
{
    public static class RenderProfileMapper
    {
        // -------------------------------
        // LIST DTO
        // -------------------------------
        public static RenderProfileListDto ToListDto(this AutoVideoRenderProfile e)
        {
            return new RenderProfileListDto
            {
                Id = e.Id,
                Name = e.Name,
                Resolution = e.Resolution,
                Fps = e.Fps,
                Style = e.Style,
                CaptionPosition = e.CaptionPosition
            };
        }

        // -------------------------------
        // DETAIL DTO
        // -------------------------------
        public static RenderProfileDetailDto ToDetailDto(this AutoVideoRenderProfile e)
        {
            return new RenderProfileDetailDto
            {
                Id = e.Id,
                Name = e.Name,

                // RES & FPS
                Resolution = e.Resolution,
                Fps = e.Fps,

                // STYLE
                Style = e.Style,

                // CAPTIONS
                CaptionStyle = e.CaptionStyle,
                CaptionFont = e.CaptionFont,
                CaptionSize = e.CaptionSize,
                CaptionGlow = e.CaptionGlow,
                CaptionGlowColor = e.CaptionGlowColor,
                CaptionGlowSize = e.CaptionGlowSize,
                CaptionOutlineSize = e.CaptionOutlineSize,
                CaptionShadowSize = e.CaptionShadowSize,
                CaptionKaraoke = e.CaptionKaraoke,
                CaptionHighlightColor = e.CaptionHighlightColor,
                CaptionChunkSize = e.CaptionChunkSize,
                CaptionPosition = e.CaptionPosition,
                CaptionMarginV = e.CaptionMarginV,
                CaptionLineSpacing = e.CaptionLineSpacing,
                CaptionMaxWidthPercent = e.CaptionMaxWidthPercent,
                CaptionBackgroundOpacity = e.CaptionBackgroundOpacity,
                CaptionAnimation = e.CaptionAnimation,

                // MOTION
                MotionIntensity = e.MotionIntensity,
                ZoomSpeed = e.ZoomSpeed,
                ZoomMax = e.ZoomMax,
                PanX = e.PanX,
                PanY = e.PanY,

                // TRANSITIONS
                Transition = e.Transition,
                TransitionDuration = e.TransitionDuration,
                TransitionDirection = e.TransitionDirection,
                TransitionEasing = e.TransitionEasing,
                TransitionStrength = e.TransitionStrength,

                // TIMELINE
                TimelineMode = e.TimelineMode,

                // AUDIO
                BgmVolume = e.BgmVolume,
                VoiceVolume = e.VoiceVolume,
                DuckingStrength = e.DuckingStrength,

                // AI RECOMMENDATIONS
                AiRecommendedStyle = e.AiRecommendedStyle,
                AiRecommendedTransitions = e.AiRecommendedTransitions,
                AiRecommendedCaption = e.AiRecommendedCaption
            };
        }
    }
}
