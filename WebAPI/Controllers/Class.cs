using Application.Services;
using Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JobExecutionsController : ControllerBase
    {
        private readonly JobExecutionService _svc;
        private readonly ICurrentUserService _current;

        public JobExecutionsController(JobExecutionService svc, ICurrentUserService current)
        {
            _svc = svc;
            _current = current;
        }

        /// <summary>
        /// JobExecution listesini getirir. (Opsiyonel filtre: jobId)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int? jobId, CancellationToken ct)
        {
            var list = await _svc.ListAsync(jobId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Tek bir JobExecution detayını getirir.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }
    }
}
