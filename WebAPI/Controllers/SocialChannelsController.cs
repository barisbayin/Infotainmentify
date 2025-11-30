using Application.Contracts.UserSocialChannel;
using Application.Extensions;
using Application.Mappers;
using Application.Services;
using Core.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/social-channels")]
    public class SocialChannelsController : ControllerBase
    {
        private readonly SocialChannelService _service;

        public SocialChannelsController(SocialChannelService service)
        {
            _service = service;
        }

        // =================================================================
        // LIST
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            // 1. Servisten Entity Listesi Al (BaseService.GetAllAsync)
            var entities = await _service.GetAllAsync(User.GetUserId(), ct);

            // 2. Mapper ile DTO'ya çevir
            var dtos = entities.Select(x => x.ToListDto());

            return Ok(dtos);
        }

        // =================================================================
        // GET (Detail)
        // =================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            // 1. Servisten Entity Al (BaseService.GetByIdAsync)
            var entity = await _service.GetByIdAsync(id, User.GetUserId(), ct);

            if (entity == null) return NotFound();

            // 2. Mapper ile DTO'ya çevir (Token durumu vs. burada hesaplanıyor)
            return Ok(entity.ToDetailDto());
        }

        // =================================================================
        // CREATE
        // =================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveSocialChannelDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // 1. DTO -> Entity Mapping (Manuel)
            var entity = new SocialChannel
            {
                ChannelType = dto.ChannelType,
                ChannelName = dto.ChannelName,
                ChannelHandle = dto.ChannelHandle,
                ChannelUrl = dto.ChannelUrl,
                PlatformChannelId = dto.PlatformChannelId,
                TokenExpiresAt = dto.TokenExpiresAt,
                Scopes = dto.Scopes
                // EncryptedTokensJson serviste set edilecek
            };

            // 2. Servisi Çağır (Şifreleme işini servis yapacak)
            await _service.CreateChannelAsync(entity, dto.RawTokensJson, userId, ct);

            return CreatedAtAction(nameof(Get), new { id = entity.Id }, new { id = entity.Id });
        }

        // =================================================================
        // UPDATE
        // =================================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SaveSocialChannelDto dto, CancellationToken ct)
        {
            int userId = User.GetUserId();

            // 1. Mevcut Entity'i Çek
            var entity = await _service.GetByIdAsync(id, userId, ct);
            if (entity == null) return NotFound();

            // 2. Alanları Güncelle (Mapping)
            entity.ChannelName = dto.ChannelName;
            entity.ChannelHandle = dto.ChannelHandle;
            entity.ChannelUrl = dto.ChannelUrl;
            entity.PlatformChannelId = dto.PlatformChannelId;
            entity.Scopes = dto.Scopes;
            entity.TokenExpiresAt = dto.TokenExpiresAt;
            // ChannelType genelde update edilmez ama istersen ekle:
            // entity.ChannelType = dto.ChannelType;

            // 3. Servise Gönder (Token güncellemesi varsa şifreler)
            await _service.UpdateChannelAsync(entity, dto.RawTokensJson, userId, ct);

            return NoContent();
        }

        // =================================================================
        // DELETE
        // =================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                // BaseService DeleteAsync
                await _service.DeleteAsync(id, User.GetUserId(), ct);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }
    }
}
