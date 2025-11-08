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

        public ScriptGenerationController(
            ScriptGenerationService svc,
            ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        /// <summary>
        /// Seçilen ScriptGenerationProfile’a göre uygun topic’ler için script üretimi başlatır.
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromQuery] int profileId, CancellationToken ct)
        {
            try
            {
                var result = await _svc.GenerateFromProfileAsync(profileId, ct);

                return Ok(new
                {
                    success = true,
                    message = $"{result.SuccessCount} script üretildi.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Script generation error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Beklenmedik bir hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// Belirli topic ID’leri için seçilen profile göre script üretimi yapar.
        /// </summary>
        [HttpPost("generate-from-topics")]
        public async Task<IActionResult> GenerateFromTopics([FromBody] GenerateFromTopicsRequest req, CancellationToken ct)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 💪 bağımsız token

            try
            {
                if (req.ProfileId <= 0)
                    return BadRequest(new { success = false, message = "Geçersiz profil ID." });

                var result = await _svc.GenerateForTopicsAsync(req.ProfileId, req.TopicIds ?? new List<int>(), cts.Token);

                return Ok(new
                {
                    success = true,
                    message = $"{result.SuccessCount} script üretildi.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Script generation error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Beklenmedik bir hata oluştu.", error = ex.Message });
            }
        }
    }

    public class GenerateFromTopicsRequest
    {
        public int ProfileId { get; set; }
        public List<int>? TopicIds { get; set; }
    }
}
