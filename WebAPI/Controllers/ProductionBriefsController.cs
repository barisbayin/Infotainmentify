using Application.Contracts.ProductionBriefs;
using Application.Extensions;
using Application.Services;
using Core.Contracts;
using Core.Entity.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/production-briefs")]
    public class ProductionBriefsController : ControllerBase
    {
        private readonly IRepository<SavedProductionBrief> _repo;
        private readonly IRepository<Concept> _conceptRepo;
        private readonly IUnitOfWork _uow;

        public ProductionBriefsController(
            IRepository<SavedProductionBrief> repo,
            IRepository<Concept> conceptRepo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _conceptRepo = conceptRepo;
            _uow = uow;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int? conceptId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var list = await _repo.FindAsync(
                predicate: b =>
                    b.AppUserId == userId &&
                    (!conceptId.HasValue || b.ConceptId == conceptId) &&
                    (string.IsNullOrWhiteSpace(q)
                        || b.Name.Contains(q)
                        || (b.MainTitle != null && b.MainTitle.Contains(q))
                        || (b.Angle != null && b.Angle.Contains(q))),
                orderBy: b => b.UpdatedAt ?? b.CreatedAt,
                desc: true,
                include: source => source.Include(b => b.Concept),
                asNoTracking: true,
                ct: ct);

            return Ok(list.Select(SavedProductionBriefService.ToDto));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var entity = await _repo.FirstOrDefaultAsync(
                predicate: b => b.Id == id && b.AppUserId == userId,
                include: source => source.Include(b => b.Concept),
                asNoTracking: true,
                ct: ct);

            return entity is null ? NotFound() : Ok(SavedProductionBriefService.ToDto(entity));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveProductionBriefDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            await ValidateConceptAsync(dto.ConceptId, userId, ct);

            var entity = new SavedProductionBrief
            {
                AppUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            Apply(entity, dto);

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(Get), new { id = entity.Id }, new { id = entity.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveProductionBriefDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            await ValidateConceptAsync(dto.ConceptId, userId, ct);

            var entity = await _repo.FirstOrDefaultAsync(
                predicate: b => b.Id == id && b.AppUserId == userId,
                asNoTracking: false,
                ct: ct);

            if (entity == null) return NotFound();

            Apply(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var entity = await _repo.FirstOrDefaultAsync(
                predicate: b => b.Id == id && b.AppUserId == userId,
                asNoTracking: false,
                ct: ct);

            if (entity == null) return NotFound();

            _repo.Delete(entity);
            await _uow.SaveChangesAsync(ct);
            return NoContent();
        }

        private async Task ValidateConceptAsync(int? conceptId, int userId, CancellationToken ct)
        {
            if (!conceptId.HasValue) return;

            var exists = await _conceptRepo.AnyAsync(
                c => c.Id == conceptId.Value && c.AppUserId == userId,
                ct);

            if (!exists) throw new KeyNotFoundException("Konsept bulunamadi.");
        }

        private static void Apply(SavedProductionBrief entity, SaveProductionBriefDto dto)
        {
            entity.ConceptId = dto.ConceptId;
            entity.Name = Clean(dto.Name, 160, true) ?? "";
            entity.MainTitle = Clean(dto.MainTitle, 300);
            entity.Angle = Clean(dto.Angle, 1000);
            entity.Audience = Clean(dto.Audience, 500);
            entity.TargetDuration = Clean(dto.TargetDuration, 100);
            entity.MustCover = Clean(dto.MustCover, 2500);
            entity.Avoid = Clean(dto.Avoid, 1500);
            entity.Notes = Clean(dto.Notes, 2500);
        }

        private static string? Clean(string? value, int maxLength, bool required = false)
        {
            var clean = string.IsNullOrWhiteSpace(value) ? null : value.Replace("\r", " ").Trim();
            if (clean == null)
            {
                if (required) throw new InvalidOperationException("Brief adi zorunludur.");
                return null;
            }

            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }
    }
}
