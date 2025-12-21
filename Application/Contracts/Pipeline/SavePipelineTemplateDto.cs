using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Pipeline
{
    // SAVE (Create/Update)
    public class SavePipelineTemplateDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public int ConceptId { get; set; }

        // Kullanıcı sıralı bir liste gönderir
        public List<SaveStageConfigDto> Stages { get; set; } = new();

        public bool AutoPublish { get; set; }
    }
}
