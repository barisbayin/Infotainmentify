using Application.Contracts.ConceptProfiles;
using Application.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public sealed record ProductionTargetPlan(
        int DurationSec,
        string DurationLabel,
        string Source,
        int MinNarrationWords,
        int IdealNarrationWords,
        int MaxNarrationWords,
        int MinSceneCount,
        int IdealSceneCount,
        int MaxSceneCount)
    {
        public bool IsLongForm => DurationSec >= 300;

        public string ToPromptBlock()
        {
            var sb = new StringBuilder();
            sb.AppendLine("PRODUCTION TARGET CONTRACT");
            sb.AppendLine($"Source: {Source}");
            sb.AppendLine($"Target duration: {DurationLabel}");
            sb.AppendLine($"Target duration seconds: {DurationSec}");
            sb.AppendLine($"Spoken narration words: minimum {MinNarrationWords}, ideal around {IdealNarrationWords}, maximum {MaxNarrationWords}.");
            sb.AppendLine($"Script scene count: minimum {MinSceneCount}, ideal around {IdealSceneCount}, maximum {MaxSceneCount}.");
            sb.AppendLine("The script must satisfy this contract with actual audioText length, not only durationSec numbers.");
            return sb.ToString().Trim();
        }
    }

    public static class ProductionTarget
    {
        public static ProductionTargetPlan Resolve(
            ProductionBrief? brief,
            ConceptProfileDto? conceptProfile,
            int fallbackSeconds)
        {
            var source = "preset fallback";
            var rawDuration = "";
            var fallback = fallbackSeconds > 0 ? fallbackSeconds : 600;

            if (!string.IsNullOrWhiteSpace(brief?.TargetDuration))
            {
                rawDuration = brief.TargetDuration.Trim();
                source = "production brief";
            }
            else if (conceptProfile?.DefaultDurationSec is > 0)
            {
                rawDuration = $"{conceptProfile.DefaultDurationSec.Value} seconds";
                fallback = conceptProfile.DefaultDurationSec.Value;
                source = "concept profile";
            }

            var durationSec = ResolveTargetDurationSeconds(rawDuration, fallback);
            var durationLabel = !string.IsNullOrWhiteSpace(rawDuration)
                ? rawDuration
                : $"{durationSec} seconds";

            return new ProductionTargetPlan(
                DurationSec: durationSec,
                DurationLabel: durationLabel,
                Source: source,
                MinNarrationWords: EstimateMinNarrationWords(durationSec),
                IdealNarrationWords: EstimateIdealNarrationWords(durationSec),
                MaxNarrationWords: EstimateMaxNarrationWords(durationSec),
                MinSceneCount: EstimateMinSceneCount(durationSec),
                IdealSceneCount: EstimateIdealSceneCount(durationSec),
                MaxSceneCount: EstimateMaxSceneCount(durationSec));
        }

        public static int ResolveTargetDurationSeconds(string? rawTargetDuration, int fallbackSeconds)
        {
            var fallback = fallbackSeconds > 0 ? fallbackSeconds : 600;
            if (string.IsNullOrWhiteSpace(rawTargetDuration)) return fallback;

            var normalized = rawTargetDuration.Trim().ToLowerInvariant();
            var numberMatches = Regex.Matches(normalized, @"\d+(?:[\.,]\d+)?");
            if (numberMatches.Count == 0) return fallback;

            var numbers = numberMatches
                .Select(match => match.Value.Replace(',', '.'))
                .Select(value => double.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
                .Where(value => value > 0)
                .ToList();

            if (numbers.Count == 0) return fallback;

            var selected = numbers.Count >= 2
                ? (numbers[0] + numbers[1]) / 2.0
                : numbers[0];

            var hasMinuteUnit = ContainsAny(normalized, "minute", "minutes", "min", "dk", "dakika");
            var hasHourUnit = ContainsAny(normalized, "hour", "hours", "saat");
            var hasSecondUnit = ContainsAny(normalized, "second", "seconds", "sec", "sn", "saniye");

            double seconds;
            if (hasHourUnit)
            {
                seconds = selected * 3600;
            }
            else if (hasMinuteUnit)
            {
                seconds = selected * 60;
            }
            else if (hasSecondUnit)
            {
                seconds = selected;
            }
            else
            {
                seconds = selected >= 240 ? selected : fallback;
            }

            return (int)Math.Round(Math.Clamp(seconds, 15, 3600));
        }

        public static int EstimateMinNarrationWords(int targetDurationSec)
            => (int)Math.Round(targetDurationSec * 2.35);

        public static int EstimateIdealNarrationWords(int targetDurationSec)
            => (int)Math.Round(targetDurationSec * 2.65);

        public static int EstimateMaxNarrationWords(int targetDurationSec)
            => (int)Math.Round(targetDurationSec * 3.05);

        public static int EstimateMinSceneCount(int targetDurationSec)
        {
            var divisor = targetDurationSec >= 300 ? 14.0 : 10.0;
            return ClampSceneCount((int)Math.Ceiling(targetDurationSec / divisor));
        }

        public static int EstimateIdealSceneCount(int targetDurationSec)
        {
            var divisor = targetDurationSec >= 300 ? 9.0 : 6.0;
            return ClampSceneCount((int)Math.Ceiling(targetDurationSec / divisor));
        }

        public static int EstimateMaxSceneCount(int targetDurationSec)
        {
            var divisor = targetDurationSec >= 300 ? 6.0 : 4.0;
            return ClampSceneCount((int)Math.Ceiling(targetDurationSec / divisor));
        }

        private static int ClampSceneCount(int value)
            => Math.Clamp(value, 1, 260);

        private static bool ContainsAny(string value, params string[] needles)
            => needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
