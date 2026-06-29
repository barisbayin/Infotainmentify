using Application.Contracts.Pipeline;
using Application.Contracts.ProductionWizard;
using Application.Extensions;
using Application.Mappers;
using Application.Models;
using Application.Services;
using Application.Services.Interfaces;
using Application.Services.Pipeline;
using Core.Contracts;
using Core.Entity.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/production-wizard")]
    public class ProductionWizardController : ControllerBase
    {
        private readonly ConceptService _conceptService;
        private readonly PipelineTemplateService _templateService;
        private readonly IContentPipelineService _pipelineService;
        private readonly IRepository<SavedProductionBrief> _briefRepo;
        private readonly IRepository<ProductionConceptProfile> _profileRepo;
        private readonly IRepository<Concept> _conceptRepo;
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IUnitOfWork _uow;

        public ProductionWizardController(
            ConceptService conceptService,
            PipelineTemplateService templateService,
            IContentPipelineService pipelineService,
            IRepository<SavedProductionBrief> briefRepo,
            IRepository<ProductionConceptProfile> profileRepo,
            IRepository<Concept> conceptRepo,
            IRepository<ContentPipelineTemplate> templateRepo,
            IUnitOfWork uow)
        {
            _conceptService = conceptService;
            _templateService = templateService;
            _pipelineService = pipelineService;
            _briefRepo = briefRepo;
            _profileRepo = profileRepo;
            _conceptRepo = conceptRepo;
            _templateRepo = templateRepo;
            _uow = uow;
        }

        [HttpGet("bootstrap")]
        public async Task<ActionResult<ProductionWizardBootstrapDto>> Bootstrap(
            [FromQuery] int? conceptId,
            [FromQuery] int? templateId,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            var concepts = await _conceptService.ListAsync(userId, null, ct);
            var conceptDtos = concepts.Select(x => x.ToListDto()).ToList();

            var selectedConceptId = ResolveConceptId(conceptId, conceptDtos);
            var profileService = CreateProfileService();
            var profile = selectedConceptId.HasValue
                ? await profileService.GetOrDefaultAsync(selectedConceptId.Value, userId, ct)
                : null;

            var templates = selectedConceptId.HasValue
                ? await _templateService.ListAsync(userId, null, selectedConceptId, ct)
                : Array.Empty<ContentPipelineTemplate>();

            var templateDtos = templates.Select(x => x.ToListDto()).ToList();
            var selectedTemplateId = ResolveTemplateId(templateId, profile?.DefaultTemplateId, templateDtos);

            var briefs = await LoadBriefsAsync(userId, selectedConceptId, ct);
            var health = selectedTemplateId.HasValue
                ? await _templateService.GetHealthAsync(selectedTemplateId.Value, userId, ct)
                : null;

            var preflight = await BuildPreflightAsync(
                userId,
                new ProductionWizardRequestDto
                {
                    ConceptId = selectedConceptId,
                    TemplateId = selectedTemplateId,
                    AutoStart = true,
                    PauseBeforeRender = true
                },
                profile,
                health,
                ct);

            return Ok(new ProductionWizardBootstrapDto
            {
                Concepts = conceptDtos,
                ConceptProfile = profile,
                Templates = templateDtos,
                Briefs = briefs,
                RecommendedConceptId = selectedConceptId,
                RecommendedTemplateId = selectedTemplateId,
                SelectedTemplateHealth = health,
                Preflight = preflight
            });
        }

        [HttpPost("preflight")]
        public async Task<ActionResult<ProductionWizardPreflightDto>> Preflight(
            [FromBody] ProductionWizardRequestDto request,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            var profile = await ResolveProfileAsync(userId, request.ConceptId, ct);
            var health = request.TemplateId.HasValue
                ? await _templateService.GetHealthAsync(request.TemplateId.Value, userId, ct)
                : null;

            return Ok(await BuildPreflightAsync(userId, request, profile, health, ct));
        }

        [HttpPost("start")]
        public async Task<ActionResult<ProductionWizardStartResultDto>> Start(
            [FromBody] ProductionWizardRequestDto request,
            CancellationToken ct)
        {
            var userId = User.GetUserId();
            var profile = await ResolveProfileAsync(userId, request.ConceptId, ct);
            var health = request.TemplateId.HasValue
                ? await _templateService.GetHealthAsync(request.TemplateId.Value, userId, ct)
                : null;
            var preflight = await BuildPreflightAsync(userId, request, profile, health, ct);

            if (!preflight.CanStart)
                return BadRequest(preflight);

            var runId = await _pipelineService.CreateRunAsync(
                userId,
                new CreatePipelineRunRequest
                {
                    TemplateId = request.TemplateId!.Value,
                    AutoStart = request.AutoStart,
                    PauseBeforeRender = request.PauseBeforeRender,
                    SavedBriefId = request.SavedBriefId,
                    Brief = request.Brief
                },
                ct);

            return Ok(new ProductionWizardStartResultDto
            {
                RunId = runId,
                Message = request.AutoStart ? "Production run started." : "Production run created.",
                Preflight = preflight
            });
        }

        private ConceptProfileService CreateProfileService()
            => new(_profileRepo, _conceptRepo, _templateRepo, _uow);

        private async Task<Application.Contracts.ConceptProfiles.ConceptProfileDto?> ResolveProfileAsync(
            int userId,
            int? conceptId,
            CancellationToken ct)
        {
            if (!conceptId.HasValue) return null;
            return await CreateProfileService().GetOrDefaultAsync(conceptId.Value, userId, ct);
        }

        private static int? ResolveConceptId(int? requestedConceptId, IReadOnlyList<Application.Contracts.Concept.ConceptListDto> concepts)
        {
            if (requestedConceptId.HasValue && concepts.Any(x => x.Id == requestedConceptId.Value))
                return requestedConceptId.Value;

            return concepts.FirstOrDefault()?.Id;
        }

        private static int? ResolveTemplateId(
            int? requestedTemplateId,
            int? profileTemplateId,
            IReadOnlyList<PipelineTemplateListDto> templates)
        {
            if (requestedTemplateId.HasValue && templates.Any(x => x.Id == requestedTemplateId.Value))
                return requestedTemplateId.Value;

            if (profileTemplateId.HasValue && templates.Any(x => x.Id == profileTemplateId.Value))
                return profileTemplateId.Value;

            return templates
                .OrderByDescending(x => string.Equals(x.ProductionProfile, "LongForm", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefault()
                ?.Id;
        }

        private async Task<IReadOnlyList<Application.Contracts.ProductionBriefs.SavedProductionBriefDto>> LoadBriefsAsync(
            int userId,
            int? conceptId,
            CancellationToken ct)
        {
            var list = await _briefRepo.FindAsync(
                predicate: b =>
                    b.AppUserId == userId &&
                    (!conceptId.HasValue || b.ConceptId == conceptId || b.ConceptId == null),
                orderBy: b => b.UpdatedAt ?? b.CreatedAt,
                desc: true,
                include: source => source.Include(b => b.Concept!),
                asNoTracking: true,
                ct: ct);

            return list.Select(SavedProductionBriefService.ToDto).ToList();
        }

        private async Task<ProductionWizardPreflightDto> BuildPreflightAsync(
            int userId,
            ProductionWizardRequestDto request,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? profile,
            PipelineTemplateHealthDto? health,
            CancellationToken ct)
        {
            var result = new ProductionWizardPreflightDto();

            ContentPipelineTemplate? template = null;
            if (!request.ConceptId.HasValue)
            {
                Add(result, "Error", "concept.missing", "Once bir konsept sec.", "concept");
            }
            else if (!await _conceptRepo.AnyAsync(c => c.Id == request.ConceptId.Value && c.AppUserId == userId, ct))
            {
                Add(result, "Error", "concept.not_found", "Secilen konsept bulunamadi.", "concept");
            }

            if (!request.TemplateId.HasValue)
            {
                Add(result, "Error", "template.missing", "Long-form uretim hatti secilmedi.", "template");
            }
            else
            {
                template = await _templateRepo.FirstOrDefaultAsync(
                    predicate: t => t.Id == request.TemplateId.Value && t.AppUserId == userId,
                    include: source => source.Include(t => t.StageConfigs),
                    asNoTracking: true,
                    ct: ct);

                if (template == null)
                {
                    Add(result, "Error", "template.not_found", "Secilen uretim hatti bulunamadi.", "template");
                }
                else if (request.ConceptId.HasValue && template.ConceptId != request.ConceptId.Value)
                {
                    Add(result, "Error", "template.concept_mismatch", "Secilen uretim hatti bu konsepte ait degil.", "template");
                }
            }

            AddProfileChecks(result, profile);
            await AddBriefChecksAsync(result, userId, request, ct);
            AddHealthChecks(result, health);

            if (template != null && !string.Equals(template.ProductionProfile, "LongForm", StringComparison.OrdinalIgnoreCase))
            {
                Add(result, "Warning", "template.profile_not_longform", "Secilen workflow LongForm profilinde degil.", "template");
            }

            if (!request.PauseBeforeRender)
            {
                Add(result, "Warning", "review.render_gate_off", "Render oncesi duraklama kapali. Uzun videoda gorselleri once kontrol etmek daha iyi.", "review");
            }

            result.ErrorCount = result.Items.Count(x => IsSeverity(x, "Error"));
            result.WarningCount = result.Items.Count(x => IsSeverity(x, "Warning"));
            result.InfoCount = result.Items.Count(x => IsSeverity(x, "Info"));
            result.CanStart = result.ErrorCount == 0;

            if (result.CanStart)
            {
                result.RecommendedNextSteps.Add("Uretimi baslatabilirsin. Gorsel ve kurgu kontrolu icin render oncesi duraklama acik kalsin.");
            }
            else
            {
                result.RecommendedNextSteps.Add("Once hata seviyesindeki maddeleri duzelt.");
            }

            if (result.WarningCount > 0)
                result.RecommendedNextSteps.Add("Warning seviyesindeki maddeler uretimi engellemez ama uzun video kalitesini etkiler.");

            return result;
        }

        private static void AddProfileChecks(
            ProductionWizardPreflightDto result,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? profile)
        {
            if (profile == null)
            {
                Add(result, "Warning", "profile.missing", "Concept profile bulunamadi. Concept Studio'da profil kaydet.", "concept-profile");
                return;
            }

            if (!profile.Exists)
                Add(result, "Warning", "profile.not_saved", "Concept profile henuz kaydedilmemis; varsayilanlarla calisacak.", "concept-profile");

            if (string.IsNullOrWhiteSpace(profile.Audience))
                Add(result, "Warning", "profile.audience_missing", "Hedef kitle bos. Brief/prompt daha daginik uretilebilir.", "concept-profile");

            if (string.IsNullOrWhiteSpace(profile.Tone))
                Add(result, "Warning", "profile.tone_missing", "Ton bos. Script tarzi tutarsiz olabilir.", "concept-profile");

            if (string.IsNullOrWhiteSpace(profile.VisualStyleBible))
                Add(result, "Warning", "profile.style_bible_missing", "Gorsel stil bible bos. Image promptlari tekduze kalabilir.", "concept-profile");

            if (!profile.DefaultTemplateId.HasValue)
                Add(result, "Info", "profile.default_template_missing", "Concept profile icinde default workflow bagli degil.", "concept-profile");
        }

        private async Task AddBriefChecksAsync(
            ProductionWizardPreflightDto result,
            int userId,
            ProductionWizardRequestDto request,
            CancellationToken ct)
        {
            SavedProductionBrief? savedBrief = null;
            if (request.SavedBriefId.HasValue)
            {
                savedBrief = await _briefRepo.FirstOrDefaultAsync(
                    predicate: b => b.Id == request.SavedBriefId.Value && b.AppUserId == userId,
                    asNoTracking: true,
                    ct: ct);

                if (savedBrief == null)
                {
                    Add(result, "Error", "brief.not_found", "Secilen kayitli brief bulunamadi.", "brief");
                    return;
                }

                if (request.ConceptId.HasValue && savedBrief.ConceptId.HasValue && savedBrief.ConceptId.Value != request.ConceptId.Value)
                {
                    Add(result, "Error", "brief.concept_mismatch", "Secilen brief farkli bir konsepte ait.", "brief");
                }
            }

            var brief = request.Brief;
            var hasInlineBrief = brief != null && !brief.IsEmpty();
            if (!hasInlineBrief && savedBrief == null)
            {
                Add(result, "Warning", "brief.missing", "Long-form icin ana baslik ve aci/tez vermek daha iyi sonuc uretir.", "brief");
                return;
            }

            if (hasInlineBrief && string.IsNullOrWhiteSpace(brief!.MainTitle))
                Add(result, "Warning", "brief.title_missing", "Brief var ama ana baslik bos.", "brief");

            if (hasInlineBrief && string.IsNullOrWhiteSpace(brief!.Angle))
                Add(result, "Info", "brief.angle_missing", "Aci/tez bos. Video daha jenerik kalabilir.", "brief");
        }

        private static void AddHealthChecks(ProductionWizardPreflightDto result, PipelineTemplateHealthDto? health)
        {
            if (health == null) return;

            foreach (var item in health.Items)
            {
                Add(result, item.Severity, $"template.{item.Code}", item.Message, item.StageType ?? "template");
            }

            foreach (var step in health.RecommendedNextSteps)
            {
                if (!string.IsNullOrWhiteSpace(step))
                    result.RecommendedNextSteps.Add(step);
            }

            if (!health.IsRunnable || health.ErrorCount > 0)
                Add(result, "Error", "template.not_runnable", "Workflow health hata veriyor; uretim baslamadan duzelt.", "template");
        }

        private static void Add(ProductionWizardPreflightDto result, string severity, string code, string message, string? target)
        {
            result.Items.Add(new ProductionWizardPreflightItemDto
            {
                Severity = NormalizeSeverity(severity),
                Code = code,
                Message = message,
                Target = target
            });
        }

        private static string NormalizeSeverity(string? severity)
            => severity?.Trim() switch
            {
                "Error" => "Error",
                "Warning" => "Warning",
                "Healthy" => "Info",
                _ => "Info"
            };

        private static bool IsSeverity(ProductionWizardPreflightItemDto item, string severity)
            => string.Equals(item.Severity, severity, StringComparison.OrdinalIgnoreCase);
    }
}
