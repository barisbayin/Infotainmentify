using System.Text.Json.Serialization;

namespace Application.Contracts.Script
{
    public class ScriptContentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en-US";

        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("series")]
        public string? Series { get; set; }

        [JsonPropertyName("genre")]
        public string? Genre { get; set; }

        [JsonPropertyName("hook")]
        public string? Hook { get; set; }

        [JsonPropertyName("music_cue")]
        public string? MusicCue { get; set; }

        [JsonPropertyName("voice")]
        public ScriptVoiceDto? Voice { get; set; }

        [JsonPropertyName("render_instructions")]
        public RenderInstructionDto? RenderInstructions { get; set; }

        [JsonPropertyName("scenes")]
        public List<SceneDto> Scenes { get; set; } = new();

        [JsonPropertyName("thumbnail")]
        public ThumbnailDto? Thumbnail { get; set; }

        [JsonPropertyName("metadata")]
        public MetadataDto? Metadata { get; set; }

        [JsonPropertyName("youtube")]
        public YoutubeDto? Youtube { get; set; }

        [JsonPropertyName("safety")]
        public SafetyDto? Safety { get; set; }

        [JsonPropertyName("policy_review")]
        public PolicyReviewDto? PolicyReview { get; set; }

        // 🔹 Final video render bilgileri
        public ScriptRenderInfo? Render { get; set; }
    }

    public class ScriptVoiceDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("rate")]
        public string? Rate { get; set; }

        [JsonPropertyName("pitch")]
        public string? Pitch { get; set; }
    }

    public class RenderInstructionDto
    {
        [JsonPropertyName("fps")]
        public int Fps { get; set; }

        [JsonPropertyName("style")]
        public string? Style { get; set; }

        [JsonPropertyName("transition")]
        public string? Transition { get; set; }

        [JsonPropertyName("color_palette")]
        public string? ColorPalette { get; set; }

        [JsonPropertyName("caption_font")]
        public string? CaptionFont { get; set; }

        [JsonPropertyName("music_theme")]
        public string? MusicTheme { get; set; }

        [JsonPropertyName("sound_mix")]
        public SoundMixDto? SoundMix { get; set; }
    }

    public class SoundMixDto
    {
        [JsonPropertyName("voice_volume")]
        public int VoiceVolume { get; set; }

        [JsonPropertyName("bgm_volume")]
        public int BgmVolume { get; set; }

        [JsonPropertyName("crossfade_duration")]
        public double CrossfadeDuration { get; set; }
    }

    public class SceneDto
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("seconds")]
        public double Seconds { get; set; }

        [JsonPropertyName("narration")]
        public string Narration { get; set; } = default!;

        [JsonPropertyName("emotion_curve")]
        public string? EmotionCurve { get; set; }

        [JsonPropertyName("sound_cue")]
        public string? SoundCue { get; set; }

        [JsonPropertyName("camera_cue")]
        public string? CameraCue { get; set; }

        [JsonPropertyName("tts_emphasis")]
        public string? TtsEmphasis { get; set; }

        [JsonPropertyName("subtitles")]
        public List<SubtitleDto> Subtitles { get; set; } = new();

        [JsonPropertyName("imagePrompt")]
        public string? ImagePrompt { get; set; }

        [JsonPropertyName("negativePrompt")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("imageGeneratedPath")]
        public string? ImageGeneratedPath { get; set; }

        [JsonPropertyName("audioGeneratedPath")]
        public string? AudioGeneratedPath { get; set; }

        [JsonPropertyName("videoGeneratedPath")]
        public string? VideoGeneratedPath { get; set; }
    }

    public class SubtitleDto
    {
        [JsonPropertyName("t")]
        public double T { get; set; }

        [JsonPropertyName("d")]
        public double D { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;
    }

    public class ThumbnailDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("guidance")]
        public string? Guidance { get; set; }
    }

    public class MetadataDto
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("playlist")]
        public string? Playlist { get; set; }
    }

    public class YoutubeDto
    {
        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("audience")]
        public string? Audience { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("ai_disclosure")]
        public string? AiDisclosure { get; set; }

        [JsonPropertyName("upload_policy")]
        public UploadPolicyDto? UploadPolicy { get; set; }
    }

    public class UploadPolicyDto
    {
        [JsonPropertyName("safe_for_ads")]
        public bool SafeForAds { get; set; }

        [JsonPropertyName("allow_comments")]
        public bool AllowComments { get; set; }

        [JsonPropertyName("allow_embedding")]
        public bool AllowEmbedding { get; set; }

        [JsonPropertyName("monetization")]
        public string? Monetization { get; set; }
    }

    public class SafetyDto
    {
        [JsonPropertyName("violence")]
        public string? Violence { get; set; }

        [JsonPropertyName("self_harm")]
        public string? SelfHarm { get; set; }

        [JsonPropertyName("privacy")]
        public string? Privacy { get; set; }
    }

    public class PolicyReviewDto
    {
        [JsonPropertyName("youtube_safe")]
        public bool YoutubeSafe { get; set; }

        [JsonPropertyName("violence_score")]
        public double ViolenceScore { get; set; }

        [JsonPropertyName("shock_factor")]
        public double ShockFactor { get; set; }

        [JsonPropertyName("recommended_audience")]
        public string? RecommendedAudience { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Final render işleminin çıktı bilgilerini taşır.
    /// </summary>
    public class ScriptRenderInfo
    {
        public string FilePath { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public double? DurationSeconds { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? Resolution { get; set; }
        public string? Format { get; set; } = "mp4";
        public string? ThumbnailPath { get; set; }
    }
}


