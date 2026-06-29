using Application.Contracts.Concept;
using Application.Contracts.ConceptProfiles;
using Application.Contracts.Mappers;
using Application.Extensions;
using Application.Mappers;
using Application.Services;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity.Pipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConceptsController : ControllerBase
    {
        private readonly ConceptService _service;
        private readonly IRepository<ProductionConceptProfile> _profileRepo;
        private readonly IRepository<Concept> _conceptRepo;
        private readonly IRepository<ContentPipelineTemplate> _templateRepo;
        private readonly IUnitOfWork _uow;

        public ConceptsController(
            ConceptService service,
            IRepository<ProductionConceptProfile> profileRepo,
            IRepository<Concept> conceptRepo,
            IRepository<ContentPipelineTemplate> templateRepo,
            IUnitOfWork uow)
        {
            _service = service;
            _profileRepo = profileRepo;
            _conceptRepo = conceptRepo;
            _templateRepo = templateRepo;
            _uow = uow;
        }

        // GET: api/concepts
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
        {
            var list = await _service.ListAsync(User.GetUserId(), q, ct);
            // Mapper Extension kullanıyoruz
            return Ok(list.Select(x => x.ToListDto()));
        }

        // GET: api/concepts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var entity = await _service.GetByIdAsync(id, User.GetUserId(), ct);
            return entity is null ? NotFound() : Ok(entity.ToDetailDto());
        }

        [HttpGet("{id:int}/profile")]
        public async Task<IActionResult> GetProfile(int id, CancellationToken ct)
        {
            var service = new ConceptProfileService(_profileRepo, _conceptRepo, _templateRepo, _uow);
            var profile = await service.GetOrDefaultAsync(id, User.GetUserId(), ct);
            return profile is null ? NotFound() : Ok(profile);
        }

        [HttpPut("{id:int}/profile")]
        public async Task<IActionResult> SaveProfile(int id, [FromBody] SaveConceptProfileDto dto, CancellationToken ct)
        {
            var service = new ConceptProfileService(_profileRepo, _conceptRepo, _templateRepo, _uow);
            var profile = await service.SaveAsync(id, dto, User.GetUserId(), ct);
            return Ok(profile);
        }

        // POST: api/concepts (SaveConceptDto kullanıyoruz)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveConceptDto dto, CancellationToken ct)
        {
            var id = await _service.CreateAsync(dto, User.GetUserId(), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        // PUT: api/concepts/{id} (Update için PUT şart)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveConceptDto dto, CancellationToken ct)
        {
            await _service.UpdateAsync(id, dto, User.GetUserId(), ct);
            return NoContent();
        }

        // DELETE: api/concepts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}
