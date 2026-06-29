using System.Text;

namespace Application.Models
{
    public class CreativeDirectorStagePayload
    {
        public string DirectorVersion { get; set; } = "v1";
        public string VideoPromise { get; set; } = "";
        public string CoreQuestion { get; set; } = "";
        public string ViewerProfile { get; set; } = "";
        public string NarrativeAngle { get; set; } = "";
        public string Tone { get; set; } = "";
        public string HookStrategy { get; set; } = "";
        public string RetentionStrategy { get; set; } = "";
        public string VisualStrategy { get; set; } = "";
        public string PacingStrategy { get; set; } = "";
        public string EmotionalArc { get; set; } = "";
        public string Payoff { get; set; } = "";
        public string AvoidNotes { get; set; } = "";
        public List<string> MustCover { get; set; } = new();
        public List<string> VisualFormats { get; set; } = new();
        public List<CreativeDirectorChapterPlan> Chapters { get; set; } = new();

        public string ToPromptBlock()
        {
            var sb = new StringBuilder();
            sb.AppendLine("CREATIVE DIRECTOR PLAN");
            Append(sb, "Video promise", VideoPromise);
            Append(sb, "Core question", CoreQuestion);
            Append(sb, "Viewer profile", ViewerProfile);
            Append(sb, "Narrative angle", NarrativeAngle);
            Append(sb, "Tone", Tone);
            Append(sb, "Hook strategy", HookStrategy);
            Append(sb, "Retention strategy", RetentionStrategy);
            Append(sb, "Visual strategy", VisualStrategy);
            Append(sb, "Pacing strategy", PacingStrategy);
            Append(sb, "Emotional arc", EmotionalArc);
            Append(sb, "Payoff", Payoff);
            Append(sb, "Avoid notes", AvoidNotes);
            AppendList(sb, "Must cover", MustCover);
            AppendList(sb, "Visual formats", VisualFormats);

            if (Chapters.Count > 0)
            {
                sb.AppendLine("Chapter plan:");
                foreach (var chapter in Chapters.OrderBy(x => x.ChapterIndex))
                {
                    sb.AppendLine($"- {chapter.ChapterIndex}. {chapter.Title}");
                    Append(sb, "  Purpose", chapter.Purpose);
                    Append(sb, "  Viewer question", chapter.ViewerQuestion);
                    Append(sb, "  Emotional beat", chapter.EmotionalBeat);
                    Append(sb, "  Visual direction", chapter.VisualDirection);
                    Append(sb, "  Pacing", chapter.Pacing);
                }
            }

            return sb.ToString().Trim();
        }

        private static void Append(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{label}: {value.Trim()}");
        }

        private static void AppendList(StringBuilder sb, string label, List<string> values)
        {
            var clean = values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            if (clean.Count == 0) return;

            sb.AppendLine($"{label}:");
            foreach (var item in clean)
                sb.AppendLine($"- {item}");
        }
    }

    public class CreativeDirectorChapterPlan
    {
        public int ChapterIndex { get; set; } = 1;
        public string Title { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string ViewerQuestion { get; set; } = "";
        public string EmotionalBeat { get; set; } = "";
        public string VisualDirection { get; set; } = "";
        public string Pacing { get; set; } = "balanced";
        public List<string> KeyPoints { get; set; } = new();
    }
}
