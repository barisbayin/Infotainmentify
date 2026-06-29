using System.ComponentModel.DataAnnotations;

using Application.Models;

namespace Application.Contracts.Pipeline
{
    // CREATE REQUEST
    public class CreatePipelineRunRequest
    {
        [Required]
        public int TemplateId { get; set; }

        // True ise oluşturur oluşturmaz çalışmaya başlar
        public bool AutoStart { get; set; } = true;

        // True ise render aşamasına gelince durur; görsel/timeline kontrolünden sonra onayla devam eder.
        public bool PauseBeforeRender { get; set; } = true;

        // Kayitli brief secildiyse buradan gelir. Brief doluysa anlik form degerleri snapshot olarak onceliklidir.
        public int? SavedBriefId { get; set; }

        // Long-form üretimde kullanıcıdan gelen ana başlık ve üretim brief'i.
        public ProductionBrief? Brief { get; set; }
    }
}
