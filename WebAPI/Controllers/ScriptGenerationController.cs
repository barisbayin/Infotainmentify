using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ScriptGenerationController : ControllerBase
    {
        private readonly ScriptGenerationService _svc;
        private readonly ICurrentUserService _current;

        public ScriptGenerationController(ScriptGenerationService svc, ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        /// <summary>
        /// Belirtilen ScriptGenerationProfile üzerinden AI script üretimini başlatır.
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromQuery] int profileId, CancellationToken ct)
        {
            var result = await _svc.GenerateFromProfileAsync(profileId, ct);
            return Ok(new { message = result });
        }

        /// <summary>
        /// UI’dan doğrudan topic veya topicId listesi gönderilerek script üretimi yapılabilir.
        /// </summary>
        [HttpPost("generate-from-topics")]
        public async Task<IActionResult> GenerateFromTopics(
            [FromBody] GenerateFromTopicsRequest req,
            CancellationToken ct)
        {
            // GenerateFromProfileAsync'e benzer ama profile olmadan direkt çalışır.
            // Burada, UI'dan gönderilen topic listesi ve profileId alınır.
            if (req.ProfileId <= 0)
                return BadRequest(new { message = "Geçerli bir profileId gereklidir." });

            // Parametreleri servis tarafında değerlendirmek istersen,
            // GenerateFromProfileAsync overload olarak genişletilebilir.
            var result = await _svc.GenerateFromProfileAsync(req.ProfileId, ct);
            return Ok(new { message = result });
        }

        public class GenerateFromTopicsRequest
        {
            public int ProfileId { get; set; }
            public List<int>? TopicIds { get; set; }
        }
    }
}
