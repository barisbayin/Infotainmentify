namespace Application.Models
{
    public class StoryboardStagePayload
    {
        public int ScriptId { get; set; }
        public string DirectorVersion { get; set; } = "v2";
        public string StyleBible { get; set; } = "";
        public string VideoMood { get; set; } = "";
        public string VisualContinuityBible { get; set; } = "";
        public string ColorPalette { get; set; } = "";
        public string CameraLanguage { get; set; } = "";
        public string LightingStyle { get; set; } = "";
        public string EditingPrinciples { get; set; } = "";
        public string NegativeVisualRules { get; set; } = "";
        public string ChapterStrategy { get; set; } = "";
        public List<DirectorChapterPlan> Chapters { get; set; } = new();
        public List<StoryboardScenePlan> Scenes { get; set; } = new();
    }

    public class DirectorChapterPlan
    {
        public int ChapterIndex { get; set; } = 1;
        public string Title { get; set; } = "";
        public string Purpose { get; set; } = "";
        public int StartSceneNumber { get; set; } = 1;
        public int EndSceneNumber { get; set; } = 1;
        public string Pacing { get; set; } = "balanced";
        public string VisualMotif { get; set; } = "";
        public string MusicEnergy { get; set; } = "medium";
    }

    public class StoryboardScenePlan
    {
        public int SceneNumber { get; set; }
        public int ChapterIndex { get; set; } = 1;
        public string ChapterTitle { get; set; } = "";
        public string SceneType { get; set; } = "explanation";
        public string ScenePurpose { get; set; } = "";
        public string RetentionGoal { get; set; } = "";
        public string EmotionalTone { get; set; } = "curious";
        public string VisualContrast { get; set; } = "";
        public string ContinuityAnchor { get; set; } = "";
        public string MusicEnergy { get; set; } = "medium";
        public string CaptionMode { get; set; } = "word_sync";
        public string TransitionType { get; set; } = "cut";
        public string OverlayText { get; set; } = "";
        public string SoundCue { get; set; } = "";
        public List<StoryboardVisualBeat> VisualBeats { get; set; } = new();
    }

    public class StoryboardVisualBeat
    {
        public int BeatIndex { get; set; } = 1;
        public string BeatRole { get; set; } = "primary";
        public string ShotType { get; set; } = "medium shot";
        public string CameraMotion { get; set; } = "slow_push_in";
        public string Subject { get; set; } = "";
        public string Composition { get; set; } = "";
        public string Lens { get; set; } = "";
        public string Lighting { get; set; } = "";
        public string ColorNotes { get; set; } = "";
        public string ContinuityNotes { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public string CutIntent { get; set; } = "";
        public string VisualPrompt { get; set; } = "";
        public string OnScreenText { get; set; } = "";
        public double DurationWeight { get; set; } = 1.0;
    }
}
