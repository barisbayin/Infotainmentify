using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Pipeline
{
    public class SaveStageConfigDto
    {
        [Required]
        public StageType StageType { get; set; }

        public int Order { get; set; }
        public int? PresetId { get; set; }
        public string? OptionsJson { get; set; }
    }
}
