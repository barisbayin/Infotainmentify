using Application.Services;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AutoVideoAssetsController : ControllerBase
    {
        private readonly AutoVideoAssetService _svc;
        private readonly IRepository<AutoVideoAsset> _repo;
        private readonly ICurrentUserService _current;

        public AutoVideoAssetsController(
            AutoVideoAssetService svc,
            IRepository<AutoVideoAsset> repo,
            ICurrentUserService current)
        {
            _svc = svc;
            _repo = repo;
            _current = current;
        }

        // ---------------------------------------------------
        // LIST
        // ---------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var items = await _repo.FindAsync(
                x => x.AppUserId == _current.UserId,
                asNoTracking: true,
                ct: ct);

            return Ok(items.Select(x => new
            {
                x.Id,
                x.ProfileId,
                x.TopicId,
                x.ScriptId,
                x.VideoPath,
                x.ThumbnailPath,
                x.Uploaded,
                x.UploadVideoId,
                x.UploadPlatform,
                x.Status,
                x.CreatedAt
            }));
        }

        // ---------------------------------------------------
        // GET DETAIL
        // ---------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.AppUserId == _current.UserId,
                asNoTracking: true,
                ct: ct);

            if (e == null)
                return NotFound(new { message = "AutoVideoAsset bulunamadı." });

            return Ok(e);
        }

        // ---------------------------------------------------
        // GET LOG (JSON)
        // ---------------------------------------------------
        [HttpGet("{id:int}/log")]
        public async Task<IActionResult> GetLog(int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.AppUserId == _current.UserId,
                asNoTracking: true,
                ct: ct);

            if (e == null)
                return NotFound(new { message = "AutoVideoAsset bulunamadı." });

            if (string.IsNullOrWhiteSpace(e.Log))
                return Ok(Array.Empty<object>());

            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<object>(e.Log!);
                return Ok(json);
            }
            catch
            {
                return Ok(new[] { new { error = "Invalid log JSON", raw = e.Log } });
            }
        }

        // ---------------------------------------------------
        // DELETE (opsiyonel)
        // ---------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                return NotFound(new { message = "AutoVideoAsset bulunamadı." });

            _repo.Delete(e);
            await _repo.SaveChangesAsync(ct);

            return Ok(new { success = true });
        }
    }
}
