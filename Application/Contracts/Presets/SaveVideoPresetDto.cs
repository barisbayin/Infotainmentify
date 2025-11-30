using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Presets
{

    public class SaveVideoPresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public int UserAiConnectionId { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "runway-gen-3-alpha";

        [Required]
        public VideoGenerationMode GenerationMode { get; set; } = VideoGenerationMode.ImageToVideo;

        [Required, MaxLength(20)]
        public string AspectRatio { get; set; } = "9:16";

        [Range(1, 60)]
        public int DurationSeconds { get; set; } = 5;

        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = default!;

        [MaxLength(2000)]
        public string? NegativePrompt { get; set; }

        public string? CameraControlSettingsJson { get; set; }
        public string? AdvancedSettingsJson { get; set; }
    }
}
