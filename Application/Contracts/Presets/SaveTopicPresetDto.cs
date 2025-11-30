using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Presets
{
    // =================================================================
    // 3. SAVE DTO (Create & Update İçin)
    // =================================================================
    public class SaveTopicPresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public int UserAiConnectionId { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "gpt-4o";

        [Range(0.0, 2.0)]
        public float Temperature { get; set; } = 0.7f;

        [MaxLength(10)]
        public string Language { get; set; } = "tr-TR";

        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = default!;

        // JSON Array string olarak gelir
        public string? ContextKeywordsJson { get; set; }

        [MaxLength(2000)]
        public string? SystemInstruction { get; set; }
    }
}
