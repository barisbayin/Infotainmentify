using Application.Contracts.Topics;
using Application.Extensions;
using Application.Contracts.Mappers;
using Application.Services;
using Core.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TopicsController : ControllerBase
    {
        private readonly TopicService _svc;

        public TopicsController(TopicService svc)
        {
            _svc = svc;
        }

        // GET: api/topics?q=space&category=science
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TopicListDto>>> List(
            [FromQuery] string? q,
            [FromQuery] string? category,
            CancellationToken ct)
        {
            var list = await _svc.ListAsync(User.GetUserId(), q, category, ct);
            return Ok(list.Select(x => x.ToListDto()));
        }

        // GET: api/topics/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TopicDetailDto>> Get(int id, CancellationToken ct)
        {
            var topic = await _svc.GetByIdAsync(id, User.GetUserId(), ct);
            return topic is null ? NotFound() : Ok(topic.ToDetailDto());
        }

        // POST: api/topics (Create)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveTopicDto dto, CancellationToken ct)
        {
            var topic = new Topic
            {
                Title = dto.Title,
                Premise = dto.Premise,
                LanguageCode = dto.LanguageCode,
                Category = dto.Category,
                SubCategory = dto.SubCategory,
                Series = dto.Series,
                TagsJson = dto.TagsJson,
                Tone = dto.Tone,
                RenderStyle = dto.RenderStyle,
                VisualPromptHint = dto.VisualPromptHint,
                // AppUserId service içinde set edilir
            };

            await _svc.AddAsync(topic, User.GetUserId(), ct);

            return CreatedAtAction(nameof(Get), new { id = topic.Id }, new { id = topic.Id });
        }

        // PUT: api/topics/{id} (Update)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveTopicDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var topic = await _svc.GetByIdAsync(id, userId, ct);

            if (topic == null) return NotFound();

            // Map Changes
            topic.Title = dto.Title;
            topic.Premise = dto.Premise;
            topic.LanguageCode = dto.LanguageCode;
            topic.Category = dto.Category;
            topic.SubCategory = dto.SubCategory;
            topic.Series = dto.Series;
            topic.TagsJson = dto.TagsJson;
            topic.Tone = dto.Tone;
            topic.RenderStyle = dto.RenderStyle;
            topic.VisualPromptHint = dto.VisualPromptHint;

            await _svc.UpdateAsync(topic, userId, ct);

            return NoContent();
        }

        // DELETE: api/topics/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _svc.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}
