using Application.Contracts.Presets;
using Application.Extensions;
using Application.Services.PresetService;
using Core.Entity.Models;
using Core.Entity.Presets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Mappers.PresetMappers;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/render-presets")]
    public class RenderPresetsController : ControllerBase
    {
        private readonly RenderPresetService _service;

        public RenderPresetsController(RenderPresetService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RenderPresetListDto>>> GetAll(CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entities = await _service.GetAllAsync(userId, ct);
            return Ok(entities.Select(e => e.ToListDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RenderPresetDetailDto>> GetById(int id, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();
            return Ok(entity.ToDetailDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveRenderPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // DTO -> Entity
            var entity = new RenderPreset
            {
                Name = dto.Name,
                OutputWidth = dto.OutputWidth,
                OutputHeight = dto.OutputHeight,
                Fps = dto.Fps,
                BitrateKbps = dto.BitrateKbps,
                ContainerFormat = dto.ContainerFormat,
            };

            // Helper Property'leri kullanarak JSON'ı otomatik oluşturuyoruz
            entity.CaptionSettings = new RenderCaptionSettings
            {
                EnableCaptions = dto.CaptionSettings.EnableCaptions,
                FontName = dto.CaptionSettings.FontName,
                FontSize = dto.CaptionSettings.FontSize,
                PrimaryColor = dto.CaptionSettings.PrimaryColor,
                OutlineColor = dto.CaptionSettings.OutlineColor,
                EnableHighlight = dto.CaptionSettings.EnableHighlight,
                HighlightColor = dto.CaptionSettings.HighlightColor,
                MaxWordsPerLine = dto.CaptionSettings.MaxWordsPerLine
            };

            entity.AudioMixSettings = new RenderAudioMixSettings
            {
                VoiceVolumePercent = dto.AudioMixSettings.VoiceVolumePercent,
                MusicVolumePercent = dto.AudioMixSettings.MusicVolumePercent,
                EnableDucking = dto.AudioMixSettings.EnableDucking
            };

            await _service.AddAsync(entity, userId, ct);
            return Ok(new { id = entity.Id, message = "Render preset created." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveRenderPresetDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            entity.Name = dto.Name;
            entity.OutputWidth = dto.OutputWidth;
            entity.OutputHeight = dto.OutputHeight;
            entity.Fps = dto.Fps;
            entity.BitrateKbps = dto.BitrateKbps;
            entity.ContainerFormat = dto.ContainerFormat;

            // Update Nested Objects
            var cap = entity.CaptionSettings; // Mevcutu al
            cap.EnableCaptions = dto.CaptionSettings.EnableCaptions;
            cap.FontName = dto.CaptionSettings.FontName;
            cap.FontSize = dto.CaptionSettings.FontSize;
            cap.PrimaryColor = dto.CaptionSettings.PrimaryColor;
            cap.OutlineColor = dto.CaptionSettings.OutlineColor;
            cap.EnableHighlight = dto.CaptionSettings.EnableHighlight;
            cap.HighlightColor = dto.CaptionSettings.HighlightColor;
            cap.MaxWordsPerLine = dto.CaptionSettings.MaxWordsPerLine;
            entity.CaptionSettings = cap; // Geri ata (JSON güncellensin)

            var aud = entity.AudioMixSettings;
            aud.VoiceVolumePercent = dto.AudioMixSettings.VoiceVolumePercent;
            aud.MusicVolumePercent = dto.AudioMixSettings.MusicVolumePercent;
            aud.EnableDucking = dto.AudioMixSettings.EnableDucking;
            entity.AudioMixSettings = aud;

            await _service.UpdateAsync(entity, userId, ct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await _service.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException) { return NotFound(); }
        }
    }
}
