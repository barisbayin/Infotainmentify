using Application.Contracts.Asset;
using Application.Extensions;
using Application.Services.Interfaces;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/assets")]
    [ApiController]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        // GET: api/assets/Music
        [HttpGet("{type}")]
        public async Task<ActionResult<List<AssetListDto>>> GetAssets(string type)
        {
            if (!Enum.TryParse<AssetType>(type, true, out var typeEnum))
                return BadRequest("Geçersiz tür.");

            int userId = User.GetUserId();
            var result = await _assetService.GetUserAssetsAsync(userId, typeEnum);

            return Ok(result); // Direkt DTO Listesi
        }

        // POST: api/assets/upload
        [HttpPost("upload")]
        public async Task<ActionResult<AssetDetailDto>> Upload([FromForm] AssetUploadDto input)
        {
            int userId = User.GetUserId();
            try
            {
                var result = await _assetService.UploadFileAsync(userId, input);
                return Ok(result); // Direkt Detay DTO
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/assets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = User.GetUserId();
            var success = await _assetService.DeleteFileAsync(userId, id);

            if (!success) return NotFound("Dosya bulunamadı.");
            return Ok(new { Message = "Silindi" });
        }
    }
}
