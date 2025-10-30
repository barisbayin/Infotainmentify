using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/topics")]
    public class TopicGeneratorController : ControllerBase
    {
        private readonly TopicGenerationService _svc;

        public TopicGeneratorController(TopicGenerationService svc)
        {
            _svc = svc;
        }

        public record GenerateRequest(int AiConnectionId, int PromptId, int Count = 10);

        //[HttpPost("generate")]
        //public async Task<IActionResult> Generate([FromBody] GenerateRequest req, CancellationToken ct)
        //{
        //    await _svc.GenerateAndSaveAsync(req.AiConnectionId, req.PromptId, req.Count, ct);
        //    return NoContent(); // ✅ DB'ye yazıldı, geri dönüş yok
        //}
    }
}
