using System.Text.Json.Serialization;

namespace Application.Contracts.Script
{
    public class ScriptContentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en-US";

        // ---- Metadata ----
        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("short_description")]
        public string? ShortDescription { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("hashtags")]
        public List<string> Hashtags { get; set; } = new();

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("genre")]
        public string? Genre { get; set; }

        // ---- Script Text ----
        [JsonPropertyName("script_text")]
        public string ScriptText { get; set; } = default!;

        // ---- Voice ----
        [JsonPropertyName("voice")]
        public ScriptVoiceDto? Voice { get; set; }

        // ---- Visual Prompts ----
        [JsonPropertyName("visual_prompts")]
        public List<VisualPromptDto> VisualPrompts { get; set; } = new();

        // ---- AI Video Prompts ----
        [JsonPropertyName("video_prompts")]
        public List<VisualPromptDto> VideoPrompts { get; set; } = new();

        // ---- Thumbnail ----
        [JsonPropertyName("thumbnail")]
        public ThumbnailDto? Thumbnail { get; set; }

        // ---- Platform Metadata (generic) ----
        [JsonPropertyName("platform")]
        public PlatformMetadataDto? Platform { get; set; }

        // ---- Render Config ----
        [JsonPropertyName("render")]
        public RenderConfigDto? Render { get; set; }

        // ---- Safety ----
        [JsonPropertyName("safety")]
        public SafetyDto? Safety { get; set; }

        // ---- Policy Review ----
        [JsonPropertyName("policy_review")]
        public PolicyReviewDto? PolicyReview { get; set; }
    }

    // =====================================================
    // VOICE DTO
    // =====================================================
    public class ScriptVoiceDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("rate")]
        public string? Rate { get; set; }

        [JsonPropertyName("pitch")]
        public string? Pitch { get; set; }
    }

    // =====================================================
    // VISUAL / VIDEO PROMPT DTO
    // =====================================================
    public class VisualPromptDto
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("negative_prompt")]
        public string? NegativePrompt { get; set; }
    }

    // =====================================================
    // THUMBNAIL DTO
    // =====================================================
    public class ThumbnailDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("guidance")]
        public string? Guidance { get; set; }
    }

    // =====================================================
    // GENERIC PLATFORM METADATA
    // =====================================================
    public class PlatformMetadataDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; } // public/unlisted/private

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("ai_disclosure")]
        public bool? AiDisclosure { get; set; }
    }

    // =====================================================
    // RENDER CONFIG
    // =====================================================
    public class RenderConfigDto
    {
        // ---------------------------------------------------------
        // 📌 Resolution & FPS
        // ---------------------------------------------------------
        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = "1080x1920"; // "720x1280" vb.

        [JsonPropertyName("fps")]
        public int Fps { get; set; } = 30;

        // ---------------------------------------------------------
        // 🎨 GENERAL STYLE (high-level preset)
        // ---------------------------------------------------------
        [JsonPropertyName("style")]
        public string? Style { get; set; }   // "viral_glow", "cinematic_dark", "neon_hyper"

        // ---------------------------------------------------------
        // 📝 CAPTION STYLE
        // ---------------------------------------------------------
        [JsonPropertyName("caption_style")]
        public string? CaptionStyle { get; set; } // "mrbeast_glow", "karaoke_yellow", "cinematic_white"

        [JsonPropertyName("caption_font")]
        public string CaptionFont { get; set; } = "Arial";

        [JsonPropertyName("caption_size")]
        public int CaptionSize { get; set; } = 48;

        [JsonPropertyName("caption_glow")]
        public bool CaptionGlow { get; set; } = true;

        [JsonPropertyName("caption_karaoke")]
        public bool CaptionKaraoke { get; set; } = false;

        // ---------------------------------------------------------
        // 🎥 MOTION (Ken Burns)
        // ---------------------------------------------------------
        [JsonPropertyName("zoom_speed")]
        public double ZoomSpeed { get; set; } = 0.00025;

        [JsonPropertyName("zoom_max")]
        public double ZoomMax { get; set; } = 1.08;

        [JsonPropertyName("pan_x")]
        public double PanX { get; set; } = 0.0;  // -1.0 → left, +1.0 → right

        [JsonPropertyName("pan_y")]
        public double PanY { get; set; } = 0.0;  // -1.0 → up, +1.0 → down

        // ---------------------------------------------------------
        // 🔥 TRANSITIONS
        // ---------------------------------------------------------
        [JsonPropertyName("transition")]
        public string Transition { get; set; } = "crossfade"; // "fade", "wipe", "slide", "zoom"

        [JsonPropertyName("transition_duration")]
        public double TransitionDuration { get; set; } = 0.3;

        // ---------------------------------------------------------
        // 🧭 TIMELINE MODES
        // ---------------------------------------------------------
        [JsonPropertyName("timeline_mode")]
        public string TimelineMode { get; set; } = "even";
        // "even" = tüm slotlar eşit  
        // "priority" = ilk img/video daha uzun  
        // "dynamic" = emotional peaks (AI çıkarır)

        // ---------------------------------------------------------
        // 🎵 MUSIC (BGM)
        // ---------------------------------------------------------
        [JsonPropertyName("music_theme")]
        public string? MusicTheme { get; set; }

        [JsonPropertyName("bgm_volume")]
        public int BgmVolume { get; set; } = 40;

        [JsonPropertyName("ducking_strength")]
        public int DuckingStrength { get; set; } = 30; // voice yüksekse bgm azaltır

        // ---------------------------------------------------------
        // 🔊 VOICE MIX
        // ---------------------------------------------------------
        [JsonPropertyName("voice_volume")]
        public int VoiceVolume { get; set; } = 100;

        // ---------------------------------------------------------
        // 🎬 AI Suggestions
        // ---------------------------------------------------------
        [JsonPropertyName("ai_recommended_style")]
        public string? AiRecommendedStyle { get; set; }

        [JsonPropertyName("ai_recommended_transitions")]
        public string? AiRecommendedTransitions { get; set; }

        [JsonPropertyName("ai_recommended_caption")]
        public string? AiRecommendedCaption { get; set; }
    }


    // =====================================================
    // SAFETY
    // =====================================================
    public class SafetyDto
    {
        [JsonPropertyName("violence")]
        public string? Violence { get; set; }

        [JsonPropertyName("self_harm")]
        public string? SelfHarm { get; set; }

        [JsonPropertyName("privacy")]
        public string? Privacy { get; set; }
    }

    // =====================================================
    // POLICY REVIEW
    // =====================================================
    public class PolicyReviewDto
    {
        [JsonPropertyName("youtube_safe")]
        public bool? YoutubeSafe { get; set; }

        [JsonPropertyName("violence_score")]
        public double? ViolenceScore { get; set; }

        [JsonPropertyName("shock_factor")]
        public double? ShockFactor { get; set; }

        [JsonPropertyName("recommended_audience")]
        public string? RecommendedAudience { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
