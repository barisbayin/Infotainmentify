namespace Application.Models
{
    public class EditPlanStagePayload
    {
        public int ScriptId { get; set; }
        public string PacingProfile { get; set; } = "documentary";
        public string TransitionPalette { get; set; } = "editorial_cuts";
        public string CaptionStrategy { get; set; } = "word_sync";
        public string AudioMood { get; set; } = "";
        public string DirectorSummary { get; set; } = "";
        public string VisualContinuityNotes { get; set; } = "";
        public string MusicEnergyCurve { get; set; } = "";
        public string SilenceStrategy { get; set; } = "";
        public List<EditScenePlan> Scenes { get; set; } = new();
    }

    public class EditScenePlan
    {
        public int SceneNumber { get; set; }
        public string Intent { get; set; } = "explanation";
        public string Pacing { get; set; } = "balanced";
        public double AudioDurationSec { get; set; }
        public string ChapterTitle { get; set; } = "";
        public string RetentionGoal { get; set; } = "";
        public string MusicEnergy { get; set; } = "medium";
        public string CaptionMode { get; set; } = "";
        public string ContinuityAnchor { get; set; } = "";
        public string EditorNote { get; set; } = "";
        public List<EditVisualBeatPlan> VisualBeats { get; set; } = new();
        public List<EditCaptionCue> CaptionCues { get; set; } = new();
        public List<EditAudioCue> AudioCues { get; set; } = new();
    }

    public class EditVisualBeatPlan
    {
        public int BeatIndex { get; set; } = 1;
        public int SourceImageBeatIndex { get; set; } = 1;
        public string ImagePath { get; set; } = "";
        public string VisualRole { get; set; } = "primary";
        public string SegmentRole { get; set; } = "establishing";
        public string EffectType { get; set; } = "zoom_in";
        public string TransitionType { get; set; } = "cut";
        public double TransitionDuration { get; set; } = 0.35;
        public double DurationWeight { get; set; } = 1.0;
        public string OverlayText { get; set; } = "";
        public string CutReason { get; set; } = "";
        public string Emphasis { get; set; } = "";
        public string ShotType { get; set; } = "";
        public string Composition { get; set; } = "";
        public string ContinuityNotes { get; set; } = "";
        public string DirectorIntent { get; set; } = "";
    }

    public class EditCaptionCue
    {
        public string Text { get; set; } = "";
        public double StartSec { get; set; }
        public double EndSec { get; set; }
        public string Emphasis { get; set; } = "";
    }

    public class EditAudioCue
    {
        public string Type { get; set; } = "none";
        public double AtSec { get; set; }
        public string Note { get; set; } = "";
    }
}
