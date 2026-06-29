using Application.Contracts.Concept;
using Application.Contracts.ConceptProfiles;
using Application.Contracts.Pipeline;
using Application.Contracts.ProductionBriefs;
using Application.Models;

namespace Application.Contracts.ProductionWizard
{
    public class ProductionWizardBootstrapDto
    {
        public IReadOnlyList<ConceptListDto> Concepts { get; set; } = Array.Empty<ConceptListDto>();
        public ConceptProfileDto? ConceptProfile { get; set; }
        public IReadOnlyList<PipelineTemplateListDto> Templates { get; set; } = Array.Empty<PipelineTemplateListDto>();
        public IReadOnlyList<SavedProductionBriefDto> Briefs { get; set; } = Array.Empty<SavedProductionBriefDto>();
        public int? RecommendedConceptId { get; set; }
        public int? RecommendedTemplateId { get; set; }
        public PipelineTemplateHealthDto? SelectedTemplateHealth { get; set; }
        public ProductionWizardPreflightDto Preflight { get; set; } = new();
    }

    public class ProductionWizardRequestDto
    {
        public int? ConceptId { get; set; }
        public int? TemplateId { get; set; }
        public int? SavedBriefId { get; set; }
        public ProductionBrief? Brief { get; set; }
        public bool AutoStart { get; set; } = true;
        public bool PauseBeforeRender { get; set; } = true;
    }

    public class ProductionWizardStartResultDto
    {
        public int RunId { get; set; }
        public string Message { get; set; } = "Production run created.";
        public ProductionWizardPreflightDto Preflight { get; set; } = new();
    }

    public class ProductionWizardPreflightDto
    {
        public bool CanStart { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public List<ProductionWizardPreflightItemDto> Items { get; set; } = new();
        public List<string> RecommendedNextSteps { get; set; } = new();
    }

    public class ProductionWizardPreflightItemDto
    {
        public string Severity { get; set; } = "Info";
        public string Code { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string? Target { get; set; }
    }
}
