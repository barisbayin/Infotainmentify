using System.Text;
using System.Text.Json;

namespace Application.Models
{
    public class ProductionBrief
    {
        public string MainTitle { get; set; } = "";
        public string Angle { get; set; } = "";
        public string Audience { get; set; } = "";
        public string TargetDuration { get; set; } = "";
        public string MustCover { get; set; } = "";
        public string Avoid { get; set; } = "";
        public string Notes { get; set; } = "";

        public bool IsEmpty()
            => string.IsNullOrWhiteSpace(MainTitle)
               && string.IsNullOrWhiteSpace(Angle)
               && string.IsNullOrWhiteSpace(Audience)
               && string.IsNullOrWhiteSpace(TargetDuration)
               && string.IsNullOrWhiteSpace(MustCover)
               && string.IsNullOrWhiteSpace(Avoid)
               && string.IsNullOrWhiteSpace(Notes);

        public string ToPromptBlock()
        {
            if (IsEmpty()) return "";

            var sb = new StringBuilder();
            sb.AppendLine("PRODUCTION BRIEF");
            Append(sb, "Main title", MainTitle);
            Append(sb, "Angle / thesis", Angle);
            Append(sb, "Target audience", Audience);
            Append(sb, "Target duration", TargetDuration);
            Append(sb, "Must cover", MustCover);
            Append(sb, "Avoid", Avoid);
            Append(sb, "Notes / sources", Notes);
            return sb.ToString().Trim();
        }

        public static ProductionBrief? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var brief = JsonSerializer.Deserialize<ProductionBrief>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return brief == null || brief.IsEmpty() ? null : brief;
            }
            catch
            {
                return null;
            }
        }

        public string ToJson()
            => JsonSerializer.Serialize(this);

        private static void Append(StringBuilder sb, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{label}: {value.Trim()}");
        }
    }
}
