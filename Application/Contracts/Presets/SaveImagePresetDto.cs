using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Presets
{
    // SAVE
    public class SaveImagePresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public int UserAiConnectionId { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "dall-e-3";

        [MaxLength(100)]
        public string? ArtStyle { get; set; } // "Anime", "Cinematic"

        [Required, MaxLength(20)]
        public string Size { get; set; } = "1024x1792"; // Shorts Default

        [MaxLength(20)]
        public string Quality { get; set; } = "standard";

        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = default!;

        [MaxLength(2000)]
        public string? NegativePrompt { get; set; }

        [Range(1, 4)]
        public int ImageCountPerScene { get; set; } = 1;
    }
}
