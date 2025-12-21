namespace Application.Contracts.Pipeline
{
    // --- ALT DTO ---
    public class StageConfigDto
    {
        public int Id { get; set; } // Update için gerekli olabilir
        public string StageType { get; set; } = default!; // Enum string ("Topic", "Script")
        public int Order { get; set; }
        public int? PresetId { get; set; } // Seçilen preset (Opsiyonel)
        public string? OptionsJson { get; set; } // İleride override ayarları için
    }
}
