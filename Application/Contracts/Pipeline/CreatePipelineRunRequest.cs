using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Pipeline
{
    // CREATE REQUEST
    public class CreatePipelineRunRequest
    {
        [Required]
        public int TemplateId { get; set; }

        // True ise oluşturur oluşturmaz çalışmaya başlar
        public bool AutoStart { get; set; } = true;
    }
}
