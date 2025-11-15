using Application.Services;
using Core.Abstractions;
using Infrastructure.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
   // /// <summary>
   // /// Script’lere ait asset (image, audio, video) üretim işlemlerini yönetir.
   // /// </summary>
   // [Authorize]
   // [ApiController]
   // [Route("api/[controller]")]
   // public class AssetGenerationController : ControllerBase
   // {
   //     private readonly AssetGenerationService _svc;
   //     private readonly ICurrentUserService _current;
   //     private readonly BackgroundJobRunner _runner;

   //     public AssetGenerationController(AssetGenerationService svc, ICurrentUserService current, BackgroundJobRunner runner)
   //     {
   //         _svc = svc;
   //         _current = current;
   //         _runner = runner;
   //     }

   //     // ------------------ GENERATE IMAGES ------------------
   //     /// <summary>
   //     /// 🎨 Sadece görselleri üretir.
   //     /// </summary>
   //     [HttpPost("generate-images/{scriptId:int}")]
   //     public async Task<IActionResult> GenerateImages(int scriptId, CancellationToken ct)
   //     {
   //         try
   //         {
   //             var msg = await _svc.GenerateImagesAsync(scriptId, ct);
   //             return Ok(new { scriptId, message = msg });
   //         }
   //         catch (Exception ex)
   //         {
   //             return BadRequest(new { scriptId, error = ex.Message });
   //         }
   //     }

   //     // ------------------ GENERATE AUDIOS ------------------
   //     /// <summary>
   //     /// 🎤 Sadece sesleri (TTS) üretir.
   //     /// </summary>
   //     [HttpPost("generate-audios/{scriptId:int}")]
   //     public async Task<IActionResult> GenerateAudios(int scriptId, CancellationToken ct)
   //     {
   //         try
   //         {
   //             var msg = await _svc.GenerateAudiosAsync(scriptId, ct);
   //             return Ok(new { scriptId, message = msg });
   //         }
   //         catch (Exception ex)
   //         {
   //             return BadRequest(new { scriptId, error = ex.Message });
   //         }
   //     }

   //     // ------------------ GENERATE VIDEO ------------------
   //     /// <summary>
   //     /// 🎞️ Sadece video render işlemini yürütür (mevcut image + audio’lardan).
   //     /// </summary>
   //     [HttpPost("generate-video/{scriptId:int}")]
   //     public async Task<IActionResult> GenerateVideo(int scriptId, CancellationToken ct)
   //     {
   //         try
   //         {
   //             var msg = await _svc.GenerateVideosAsync(scriptId, ct);
   //             return Ok(new { scriptId, message = msg });
   //         }
   //         catch (Exception ex)
   //         {
   //             return BadRequest(new { scriptId, error = ex.Message });
   //         }
   //     }

   //     // ------------------ GENERATE FULL ------------------
   //     /// <summary>
   //     /// 🎬 Script içeriğine göre tam üretim akışını başlatır 
   //     /// (image + audio + video render + VideoAsset kayıtları).
   //     /// </summary>
   //// 🎬 Tüm üretimi başlat (arka planda)
   //     [HttpPost("generate-full/{scriptId:int}")]
   //     public IActionResult GenerateAll(int scriptId)
   //     {
   //         // Hemen dön, job arka planda çalışsın
   //         _runner.Run(async (sp, ct) =>
   //         {
   //             var svc = sp.GetRequiredService<AssetGenerationService>();
   //             await svc.GenerateAllAsync(scriptId, ct);
   //         });

   //         return Ok(new { message = "Üretim arka planda başlatıldı.", scriptId });
   //     }
   // }
}
