using Application.Contracts.AutoVideoAsset;
using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoGenerationProfilesController : ControllerBase
    {
        private readonly VideoGenerationProfileService _service;
        private readonly ICurrentUserService _current;

        public VideoGenerationProfilesController(
            VideoGenerationProfileService service,
            ICurrentUserService current)
        {
            _service = service;
            _current = current;
        }

        // ---------------------------------------------
        // GET: List (current user)
        // ---------------------------------------------
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _service.ListAsync(_current.UserId, ct);
            return Ok(list);
        }

        // ---------------------------------------------
        // GET: Single
        // ---------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var dto = await _service.GetAsync(_current.UserId, id, ct);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }

        // ---------------------------------------------
        // POST: Create or Update
        // ---------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Upsert(
            VideoGenerationProfileDetailDto dto,
            CancellationToken ct)
        {
            var id = await _service.UpsertAsync(_current.UserId, dto, ct);
            return Ok(new { id });
        }

        // ---------------------------------------------
        // DELETE
        // ---------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(_current.UserId, id, ct);
            return Ok(new { ok });
        }
    }
}
