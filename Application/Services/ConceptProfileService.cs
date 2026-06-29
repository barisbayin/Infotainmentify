using Application.Contracts.ConceptProfiles;
using Core.Contracts;
using Core.Entity.Pipeline;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    public class ConceptProfileService
    {
        private static readonly HashSet<string> AllowedProfiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Generic",
            "Shorts",
            "LongForm",
            "Podcast"
        };

        private readonly IRepository<ProductionConceptProfile> _profileRepo;
        private readonly IRepository<Concept> _conceptRepo;
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IUnitOfWork _uow;

        public ConceptProfileService(
            IRepository<ProductionConceptProfile> profileRepo,
            IRepository<Concept> conceptRepo,
            IRepository<ContentPipelineTemplate> templateRepo,
            IUnitOfWork uow)
        {
            _profileRepo = profileRepo;
            _conceptRepo = conceptRepo;
            _templateRepo = templateRepo;
            _uow = uow;
        }

        public async Task<ConceptProfileDto?> GetOrDefaultAsync(int conceptId, int userId, CancellationToken ct)
        {
            var concept = await _conceptRepo.FirstOrDefaultAsync(
                predicate: c => c.Id == conceptId && c.AppUserId == userId,
                asNoTracking: true,
                ct: ct);

            if (concept == null) return null;

            var profile = await _profileRepo.FirstOrDefaultAsync(
                predicate: p => p.ConceptId == conceptId && p.AppUserId == userId,
                include: source => source.Include(p => p.DefaultTemplate!),
                asNoTracking: true,
                ct: ct);

            return profile == null
                ? BuildDefaultDto(concept)
                : ToDto(profile);
        }

        public async Task<ConceptProfileDto> SaveAsync(int conceptId, SaveConceptProfileDto dto, int userId, CancellationToken ct)
        {
            var concept = await _conceptRepo.FirstOrDefaultAsync(
                predicate: c => c.Id == conceptId && c.AppUserId == userId,
                asNoTracking: true,
                ct: ct);

            if (concept == null)
                throw new KeyNotFoundException("Konsept bulunamadi.");

            await ValidateDefaultTemplateAsync(dto.DefaultTemplateId, conceptId, userId, ct);

            var profile = await _profileRepo.FirstOrDefaultAsync(
                predicate: p => p.ConceptId == conceptId && p.AppUserId == userId,
                asNoTracking: false,
                ct: ct);

            if (profile == null)
            {
                profile = new ProductionConceptProfile
                {
                    AppUserId = userId,
                    ConceptId = conceptId,
                    CreatedAt = DateTime.UtcNow
                };

                await _profileRepo.AddAsync(profile, ct);
            }

            Apply(profile, dto);
            profile.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);

            var saved = await _profileRepo.FirstOrDefaultAsync(
                predicate: p => p.Id == profile.Id,
                include: source => source.Include(p => p.Concept).Include(p => p.DefaultTemplate!),
                asNoTracking: true,
                ct: ct);

            return ToDto(saved ?? profile);
        }

        public async Task<string?> BuildRunSnapshotJsonAsync(int conceptId, int userId, CancellationToken ct)
        {
            var profile = await _profileRepo.FirstOrDefaultAsync(
                predicate: p => p.ConceptId == conceptId && p.AppUserId == userId,
                include: source => source.Include(p => p.Concept).Include(p => p.DefaultTemplate!),
                asNoTracking: true,
                ct: ct);

            return profile == null
                ? null
                : JsonSerializer.Serialize(ToDto(profile));
        }

        public static ConceptProfileDto ToDto(ProductionConceptProfile entity)
        {
            return new ConceptProfileDto
            {
                Id = entity.Id,
                ConceptId = entity.ConceptId,
                ConceptName = entity.Concept?.Name,
                ProductionProfile = NormalizeProfile(entity.ProductionProfile),
                DefaultLanguage = CleanDefault(entity.DefaultLanguage, "en-US", 20),
                DefaultPlatform = CleanDefault(entity.DefaultPlatform, "YouTube", 60),
                Audience = entity.Audience,
                Tone = entity.Tone,
                ChannelPromise = entity.ChannelPromise,
                VisualStyleName = entity.VisualStyleName,
                VisualStyleBible = entity.VisualStyleBible,
                CharacterBible = entity.CharacterBible,
                TextPolicy = entity.TextPolicy,
                ContentRules = entity.ContentRules,
                DefaultDurationSec = entity.DefaultDurationSec,
                DefaultTemplateId = entity.DefaultTemplateId,
                DefaultTemplateName = entity.DefaultTemplate?.Name,
                DefaultReviewPolicyJson = entity.DefaultReviewPolicyJson,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Exists = true
            };
        }

        private static ConceptProfileDto BuildDefaultDto(Concept concept)
        {
            return new ConceptProfileDto
            {
                ConceptId = concept.Id,
                ConceptName = concept.Name,
                ProductionProfile = "LongForm",
                DefaultLanguage = "en-US",
                DefaultPlatform = "YouTube",
                VisualStyleName = "Whiteboardly Stick Figure Doodle",
                DefaultDurationSec = 600,
                TextPolicy = "Text is off by default. If explicitly allowed, use very short, large, readable handwritten text. Render overlay text should stay off unless a render preset enables it.",
                DefaultReviewPolicyJson = "{\"pauseBeforeRender\":true,\"requireImageReview\":true,\"requireThumbnailApproval\":true,\"requireUploadApproval\":true}",
                Exists = false
            };
        }

        private async Task ValidateDefaultTemplateAsync(int? templateId, int conceptId, int userId, CancellationToken ct)
        {
            if (!templateId.HasValue) return;

            var exists = await _templateRepo.AnyAsync(
                t => t.Id == templateId.Value && t.AppUserId == userId && t.ConceptId == conceptId,
                ct);

            if (!exists)
                throw new InvalidOperationException("Varsayilan uretim hatti bu konsepte ait degil.");
        }

        private static void Apply(ProductionConceptProfile entity, SaveConceptProfileDto dto)
        {
            entity.ProductionProfile = NormalizeProfile(dto.ProductionProfile);
            entity.DefaultLanguage = CleanDefault(dto.DefaultLanguage, "en-US", 20);
            entity.DefaultPlatform = CleanDefault(dto.DefaultPlatform, "YouTube", 60);
            entity.Audience = Clean(dto.Audience, 800);
            entity.Tone = Clean(dto.Tone, 800);
            entity.ChannelPromise = Clean(dto.ChannelPromise, 1200);
            entity.VisualStyleName = Clean(dto.VisualStyleName, 160);
            entity.VisualStyleBible = CleanMultiline(dto.VisualStyleBible);
            entity.CharacterBible = CleanMultiline(dto.CharacterBible);
            entity.TextPolicy = CleanMultiline(dto.TextPolicy);
            entity.ContentRules = CleanMultiline(dto.ContentRules);
            entity.DefaultDurationSec = dto.DefaultDurationSec;
            entity.DefaultTemplateId = dto.DefaultTemplateId;
            entity.DefaultReviewPolicyJson = CleanMultiline(dto.DefaultReviewPolicyJson);
        }

        private static string NormalizeProfile(string? profile)
        {
            var clean = Clean(profile, 40);
            return clean != null && AllowedProfiles.Contains(clean) ? clean : "LongForm";
        }

        private static string CleanDefault(string? value, string fallback, int maxLength)
            => Clean(value, maxLength) ?? fallback;

        private static string? Clean(string? value, int maxLength)
        {
            var clean = string.IsNullOrWhiteSpace(value) ? null : value.Replace("\r", " ").Trim();
            if (clean == null) return null;
            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }

        private static string? CleanMultiline(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Replace("\r\n", "\n").Trim();
    }
}
