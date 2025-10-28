using Application.Contracts.UserSocialChannel;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/social-channels")]
    public class UserSocialChannelsController : ControllerBase
    {
        private readonly UserSocialChannelService _svc;

        public UserSocialChannelsController(UserSocialChannelService svc)
        {
            _svc = svc;
        }

        // ------------------ LIST ------------------
        // GET: api/social-channels
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserSocialChannelListDto>>> List(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return Ok(list);
        }

        // ------------------ DETAIL ------------------
        // GET: api/social-channels/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserSocialChannelDetailDto>> Get(int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            if (dto is null)
                return NotFound();
            return Ok(dto);
        }

        // ------------------ CREATE ------------------
        // POST: api/social-channels
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] UserSocialChannelDetailDto model, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(model, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserSocialChannelDetailDto model, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, model, ct);
            return NoContent();
        }

        // ------------------ DELETE ------------------
        // DELETE: api/social-channels/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
