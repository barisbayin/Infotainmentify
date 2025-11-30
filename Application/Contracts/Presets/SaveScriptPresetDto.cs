using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Presets
{

    // SAVE (Create & Update için)
    public class SaveScriptPresetDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public int UserAiConnectionId { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; } = "gpt-4o";

        [MaxLength(50)]
        public string Tone { get; set; } = "Engaging";

        [Range(15, 300)]
        public int TargetDurationSec { get; set; } = 60;

        [MaxLength(10)]
        public string Language { get; set; } = "tr-TR";

        public bool IncludeHook { get; set; } = true;
        public bool IncludeCta { get; set; } = true;

        [Required, MaxLength(5000)]
        public string PromptTemplate { get; set; } = default!;

        [MaxLength(2000)]
        public string? SystemInstruction { get; set; }
    }
}

