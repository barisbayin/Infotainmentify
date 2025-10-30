using Application.Contracts.Job;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JobSettingsController : ControllerBase
    {
        private readonly JobSettingService _svc;

        public JobSettingsController(JobSettingService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Aktif kullanıcının tüm job listesini döner.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Belirli bir job detayını döner.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(id, ct);
            if (item is null)
                return NotFound(new { message = "Job bulunamadı." });
            return Ok(item);
        }

        /// <summary>
        /// Job kaydı oluşturur veya günceller.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] JobSettingDetailDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = await _svc.UpsertAsync(dto, ct);
            return Ok(new { id });
        }

        /// <summary>
        /// Job kaydını ve Quartz planlamasını siler.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _svc.DeleteAsync(id, ct);
            if (!deleted)
                return NotFound(new { message = "Job bulunamadı veya silinemedi." });
            return Ok(new { message = "Job silindi." });
        }

        /// <summary>
        /// Job’u manuel olarak tetikler.
        /// </summary>
        [HttpPost("{id:int}/trigger")]
        public async Task<IActionResult> Trigger(int id, CancellationToken ct)
        {
            await _svc.TriggerJobAsync(id, ct);
            return Ok(new { message = "Job manuel olarak tetiklendi." });
        }

        /// <summary>
        /// Job’un otomatik çalıştırma durumunu değiştirir (Pause/Resume).
        /// </summary>
        [HttpPost("{id:int}/toggle")]
        public async Task<IActionResult> ToggleAutoRun(int id, [FromQuery] bool enable, CancellationToken ct)
        {
            await _svc.ToggleAutoRunAsync(id, enable, ct);
            return Ok(new { message = enable ? "Otomatik çalıştırma etkinleştirildi." : "Otomatik çalıştırma devre dışı bırakıldı." });
        }
    }
}
