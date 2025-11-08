namespace Application.Models
{
    /// <summary>
    /// AI tarafından üretilen bir script sonucu.
    /// </summary>
    public class ScriptResult
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Language { get; set; }

        // 🔗 Bağlantılar
        public int? TopicId { get; set; }
        public string? TopicCode { get; set; }

        // 🧠 Meta bilgiler
        public string? Model { get; set; }
        public double Temperature { get; set; }
        public string? Provider { get; set; }
        public string? MetaJson { get; set; }

        public override string ToString()
            => $"{Title} ({Language}) - {Content[..Math.Min(60, Content.Length)]}...";
    }
}
