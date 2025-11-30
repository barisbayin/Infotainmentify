namespace Application.Contracts.Presets
{
    // =================================================================
    // 1. LIST DTO (Hafif - Grid İçin)
    // =================================================================
    public class TopicPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ModelName { get; set; } = default!; // Örn: "gpt-4"
        public string Language { get; set; } = default!;  // Örn: "tr-TR"
        public DateTime? UpdatedAt { get; set; }
    }
}
