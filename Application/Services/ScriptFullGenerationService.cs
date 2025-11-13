using Application.Abstractions;
using Application.Contracts.Script;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// Tek butonla Script'e ait tüm üretim adımlarını (Assets + Video + VideoAssets kaydı) yürüten servis.
    /// </summary>
    public class ScriptFullGenerationService
    {
        private readonly AssetGenerationService _assetGen;
        private readonly IRepository<Script> _scriptRepo;
        private readonly IRepository<VideoAsset> _videoAssetRepo;
        private readonly IUnitOfWork _uow;
        private readonly INotifierService _notifier;
        private readonly IUserDirectoryService _dirService;
        private readonly IFFmpegService _ffmpeg;

        public ScriptFullGenerationService(
            AssetGenerationService assetGen,
            IRepository<Script> scriptRepo,
            IRepository<VideoAsset> videoAssetRepo,
            IUnitOfWork uow,
            INotifierService notifier,
            IUserDirectoryService dirService,
            IFFmpegService ffmpeg)
        {
            _assetGen = assetGen;
            _scriptRepo = scriptRepo;
            _videoAssetRepo = videoAssetRepo;
            _uow = uow;
            _notifier = notifier;
            _dirService = dirService;
            _ffmpeg = ffmpeg;
        }

        public async Task<string> GenerateAllAsync(int scriptId, CancellationToken ct = default)
        {
            var script = await _scriptRepo.FirstOrDefaultAsync(
                x => x.Id == scriptId,
                include: q => q
                    .Include(x => x.ScriptGenerationProfile)
                        .ThenInclude(p => p.ImageAiConnection)
                    .Include(x => x.ScriptGenerationProfile)
                        .ThenInclude(p => p.TtsAiConnection),
                asNoTracking: false,
                ct: ct)
                ?? throw new InvalidOperationException("Script bulunamadı.");

            var userId = script.UserId;
            var dto = JsonSerializer.Deserialize<ScriptContentDto>(script.Content)
                ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

            await _notifier.JobProgressAsync(userId, script.Id, "🎨 Asset üretimi başlatılıyor...", 0);

            // 1️⃣ Görselleri üret
            await _assetGen.GenerateImagesAsync(scriptId, ct);

            // 2️⃣ Sesleri üret
            await _assetGen.GenerateAudiosAsync(scriptId, ct);

            await _notifier.JobProgressAsync(userId, script.Id, "🎬 Video render başlatılıyor...", 70);

            // 3️⃣ Videoyu oluştur
            var baseDir = _dirService.GetScriptDirectory(userId, script.Id);
            var renderDir = Path.Combine(baseDir, "renders");
            Directory.CreateDirectory(renderDir);

            var sceneVideos = new List<string>();

            foreach (var scene in dto.Scenes)
            {
                if (string.IsNullOrWhiteSpace(scene.ImageGeneratedPath) || string.IsNullOrWhiteSpace(scene.AudioGeneratedPath))
                    continue;

                var sceneVideo = Path.Combine(renderDir, $"scene_{scene.Index:D3}.mp4");

                await _ffmpeg.GenerateSceneVideoAsync(scene.ImageGeneratedPath, scene.AudioGeneratedPath, sceneVideo, ct);

                scene.VideoGeneratedPath = sceneVideo.Replace("\\", "/");

                // 🎥 VideoAsset oluştur
                await _videoAssetRepo.AddAsync(new VideoAsset
                {
                    UserId = userId,
                    ScriptId = script.Id,
                    AssetType = "video",
                    AssetKey = $"scene_{scene.Index:D3}_video",
                    FilePath = scene.VideoGeneratedPath,
                    IsGenerated = true,
                    GeneratedAt = DateTime.UtcNow
                }, ct);

                sceneVideos.Add(sceneVideo);
                await Task.Delay(300, ct);
            }

            // 4️⃣ Final video oluştur
            var finalPath = Path.Combine(renderDir, $"final_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            await _ffmpeg.ConcatVideosAsync(sceneVideos, finalPath, ct);

            // 🎞️ Final video metadata
            var duration = await _ffmpeg.GetVideoDurationAsync(finalPath, ct);
            var thumbPath = await _ffmpeg.GenerateThumbnailAsync(finalPath, renderDir, ct);

            // 🎬 Final VideoAsset
            await _videoAssetRepo.AddAsync(new VideoAsset
            {
                UserId = userId,
                ScriptId = script.Id,
                AssetType = "final",
                AssetKey = "final_render",
                FilePath = finalPath.Replace("\\", "/"),
                IsGenerated = true,
                GeneratedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    Duration = duration,
                    Thumbnail = thumbPath
                })
            }, ct);

            // Render bilgilerini dto’ya ekle
            dto.Render = new ScriptRenderInfo
            {
                FilePath = finalPath.Replace("\\", "/"),
                CreatedAt = DateTime.UtcNow,
                DurationSeconds = duration,
                Format = "mp4",
                Resolution = "1080x1920",
                ThumbnailPath = thumbPath.Replace("\\", "/")
            };

            // JSON güncelle
            script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            _scriptRepo.Update(script);
            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(userId, script.Id, true, "✅ Tüm üretim adımları tamamlandı (Assets + Video).");

            return "Tüm üretim adımları tamamlandı.";
        }
    }
}
