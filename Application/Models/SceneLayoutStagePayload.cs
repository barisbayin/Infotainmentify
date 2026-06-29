namespace Application.Models
{
    public class SceneLayoutStagePayload
    {
        // Video Teknik Özellikleri
        public int Width { get; set; }
        public int Height { get; set; }
        public int Fps { get; set; }
        public double TotalDuration { get; set; }

        // Görsel Timeline (Resimler ve Efektler)
        public List<VisualEvent> VisualTrack { get; set; } = new();

        // Ses Timeline (Konuşma ve Müzik)
        public List<AudioEvent> AudioTrack { get; set; } = new();

        // Altyazı Timeline (Burn-in Captions)
        public List<CaptionEvent> CaptionTrack { get; set; } = new();

        // Render oncesi okunabilir editor karar listesi.
        public List<EditDecisionItem> EditDecisionList { get; set; } = new();

        // B-roll / info visual layer planı. Render motoru ile UI'nin aynı kurgu niyetini okumasını sağlar.
        public List<BrollLayerItem> BrollLayerPlan { get; set; } = new();

        // Render oncesi kalite ve kontrol raporu.
        public SceneLayoutReviewReport ReviewReport { get; set; } = new();

        // 🔥 GÜNCELLENEN KISIM: Tüm stil ayarlarını tutan kapsayıcı
        public RenderStyleSettings Style { get; set; } = new();
    }

    public class SceneLayoutReviewReport
    {
        public string Status { get; set; } = "Ready"; // Ready, Review, Blocked
        public int SceneCount { get; set; }
        public int VisualCount { get; set; }
        public int AudioCount { get; set; }
        public int CaptionCount { get; set; }
        public int IssueCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public int FallbackImageCount { get; set; }
        public int DuplicateImageCount { get; set; }
        public int LowQualityImageCount { get; set; }
        public int MissingImageCount { get; set; }
        public double AverageVisualQualityScore { get; set; }
        public List<SceneLayoutReviewIssue> Issues { get; set; } = new();
    }

    public class SceneLayoutReviewIssue
    {
        public string Severity { get; set; } = "Info"; // Error, Warning, Info
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public string ActionHint { get; set; } = "";
        public int? SceneNumber { get; set; }
        public int? SegmentIndex { get; set; }
        public string ImagePath { get; set; } = "";
    }

    public class EditDecisionItem
    {
        public int Index { get; set; }
        public int SceneNumber { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration { get; set; }
        public string SegmentRole { get; set; } = "";
        public string VisualRole { get; set; } = "";
        public string VisualType { get; set; } = "";
        public string TransitionType { get; set; } = "";
        public string EffectType { get; set; } = "";
        public string CutReason { get; set; } = "";
        public string DirectorIntent { get; set; } = "";
        public string ChapterTitle { get; set; } = "";
        public string OverlayText { get; set; } = "";
        public string MusicEnergy { get; set; } = "";
        public string CaptionMode { get; set; } = "";
        public string AudioTransition { get; set; } = "";
        public double AudioOffsetSec { get; set; }
        public string ImagePath { get; set; } = "";
        public int SourceImageSceneNumber { get; set; }
        public int SourceImageBeatIndex { get; set; }
        public bool IsFallbackImage { get; set; }
    }

    public class BrollLayerItem
    {
        public int SceneNumber { get; set; }
        public int SegmentIndex { get; set; }
        public string LayerType { get; set; } = ""; // broll_motion, info_visual, contrast_visual
        public string VisualType { get; set; } = "";
        public string VisualRole { get; set; } = "";
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration { get; set; }
        public string ImagePath { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}
