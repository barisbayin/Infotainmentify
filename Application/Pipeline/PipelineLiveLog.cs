using Core.Enums;
using System.Reflection;

namespace Application.Pipeline
{
    public static class PipelineLiveLog
    {
        private static readonly string[] KnownPrefixes =
        {
            "[INFO]",
            "[UYARI]",
            "[HATA]",
            "[OK]"
        };

        public static string Info(string message) => WithPrefix("[INFO]", message);

        public static string Warning(string message) => WithPrefix("[UYARI]", message);

        public static string Error(string message) => WithPrefix("[HATA]", message);

        public static string Success(string message) => WithPrefix("[OK]", message);

        public static string WithTimestamp(string message)
        {
            var clean = string.IsNullOrWhiteSpace(message)
                ? Info("Log mesajı boş geldi.")
                : message.Trim();

            if (HasTimestampPrefix(clean))
                return clean;

            return $"{DateTime.Now:HH:mm:ss} - {clean}";
        }

        public static string StageName(StageType stageType)
        {
            return stageType switch
            {
                StageType.Topic => "Konu üretimi",
                StageType.Script => "Senaryo üretimi",
                StageType.Translation => "Çeviri",
                StageType.KeywordAnalysis => "Anahtar kelime analizi",
                StageType.Image => "Görsel üretimi",
                StageType.VideoAI => "AI video üretimi",
                StageType.Avatar => "Avatar üretimi",
                StageType.Tts => "Seslendirme",
                StageType.VoiceClone => "Ses klonlama",
                StageType.AudioMix => "Ses miksajı",
                StageType.Stt => "Altyazı / konuşma analizi",
                StageType.SceneLayout => "Kurgu planı",
                StageType.Subtitle => "Altyazı",
                StageType.Render => "Final render",
                StageType.Thumbnail => "Kapak görseli",
                StageType.Upload => "Platform yükleme",
                StageType.SocialShare => "Sosyal paylaşım",
                _ => stageType.ToString()
            };
        }

        public static string PresetName(object? preset)
        {
            if (preset == null) return "Preset yok";

            var nameProp = preset.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            var name = nameProp?.GetValue(preset)?.ToString();

            return string.IsNullOrWhiteSpace(name)
                ? preset.GetType().Name
                : name;
        }

        public static string Shorten(string? value, int maxLength = 180)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return clean.Length <= maxLength
                ? clean
                : clean[..Math.Max(0, maxLength - 3)] + "...";
        }

        private static string WithPrefix(string prefix, string message)
        {
            var clean = string.IsNullOrWhiteSpace(message)
                ? "Log mesajı boş geldi."
                : message.Trim();

            if (KnownPrefixes.Any(p => clean.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return clean;

            return $"{prefix} {clean}";
        }

        private static bool HasTimestampPrefix(string value)
        {
            return value.Length >= 11
                && char.IsDigit(value[0])
                && char.IsDigit(value[1])
                && value[2] == ':'
                && char.IsDigit(value[3])
                && char.IsDigit(value[4])
                && value[5] == ':'
                && char.IsDigit(value[6])
                && char.IsDigit(value[7])
                && value[8] == ' '
                && value[9] == '-'
                && value[10] == ' ';
        }
    }
}
