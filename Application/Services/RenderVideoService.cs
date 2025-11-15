using Application.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;

namespace Application.Services
{
    public class RenderVideoService
    {
        private readonly IFFmpegService _ffmpeg;
        private readonly IRepository<AutoVideoPipeline> _pipelineRepo;
        private readonly IRepository<AutoVideoAssetFile> _assetFileRepo;
        private readonly IUserDirectoryService _dir;
        private readonly INotifierService _notifier;
        private readonly IUnitOfWork _uow;
        private readonly IRepository<AppUser> _appUser;

        public RenderVideoService(
            IFFmpegService ffmpeg,
            IRepository<AutoVideoPipeline> pipelineRepo,
            IRepository<AutoVideoAssetFile> assetFileRepo,
            IUserDirectoryService dir,
            INotifierService notifier,
            IUnitOfWork uow,
            IRepository<AppUser> appUser)
        {
            _ffmpeg = ffmpeg;
            _pipelineRepo = pipelineRepo;
            _assetFileRepo = assetFileRepo;
            _dir = dir;
            _notifier = notifier;
            _uow = uow;
            _appUser = appUser;
        }

        public async Task<string> RenderVideoAsync(int pipelineId, CancellationToken ct = default)
        {
            var pipeline = await _pipelineRepo.GetByIdAsync(pipelineId, false, ct)
                ?? throw new InvalidOperationException("Pipeline bulunamadı.");

            var userId = pipeline.AppUserId;

            // Scene asset’leri çek
            var sceneAssets = await _assetFileRepo.FindAsync(
                x => x.AutoVideoPipelineId == pipelineId &&
                     (x.FileType == AutoVideoAssetFileType.Image ||
                      x.FileType == AutoVideoAssetFileType.Audio),
                asNoTracking: true,
                ct: ct
            );

            if (!sceneAssets.Any())
                throw new InvalidOperationException("Render etmek için sahne asset'i bulunamadı.");

            var user = await _appUser.GetByIdAsync(userId, false, ct)
                ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

            var renderDir = Path.Combine(_dir.GetUserRoot(user), "renders", $"pipeline_{pipelineId}");
            Directory.CreateDirectory(renderDir);

            var sceneCount = sceneAssets.Select(x => x.SceneNumber).Distinct().Count();
            int processed = 0;
            var sceneVideos = new List<string>();

            foreach (var sceneId in sceneAssets.Select(x => x.SceneNumber).Distinct())
            {
                processed++;
                var progress = (int)((double)processed / sceneCount * 100);

                await _notifier.JobProgressAsync(userId, pipelineId,
                    $"🎬 Render sahne {sceneId}", progress);

                var img = sceneAssets.FirstOrDefault(x => x.SceneNumber == sceneId &&
                                                          x.FileType == AutoVideoAssetFileType.Image)?.FilePath;
                var aud = sceneAssets.FirstOrDefault(x => x.SceneNumber == sceneId &&
                                                          x.FileType == AutoVideoAssetFileType.Audio)?.FilePath;

                if (img == null || aud == null)
                    throw new InvalidOperationException($"Sahne {sceneId} için eksik medya.");

                var output = Path.Combine(renderDir, $"scene_{sceneId:D3}.mp4");

                await _ffmpeg.GenerateSceneVideoAsync(img, aud, output, ct);

                sceneVideos.Add(output);

                // DB’ye sahne video asset kaydı
                await _assetFileRepo.AddAsync(new AutoVideoAssetFile
                {
                    AppUserId = userId,
                    AutoVideoPipelineId = pipelineId,
                    FilePath = output.Replace("\\", "/"),
                    FileType = AutoVideoAssetFileType.RenderedScene,
                    SceneNumber = sceneId
                }, ct);
            }

            // FINAL CONCAT
            var finalPath = Path.Combine(renderDir, $"final_{DateTime.Now:yyyyMMddHHmmss}.mp4");
            await _ffmpeg.ConcatVideosAsync(sceneVideos, finalPath, ct);

            // Thumbnail
            var thumb = await _ffmpeg.GenerateThumbnailAsync(finalPath, renderDir, ct);

            await _assetFileRepo.AddAsync(new AutoVideoAssetFile
            {
                AppUserId = userId,
                AutoVideoPipelineId = pipelineId,
                FilePath = finalPath.Replace("\\", "/"),
                FileType = AutoVideoAssetFileType.FinalVideo,
                SceneNumber = 0
            }, ct);

            pipeline.VideoPath = finalPath.Replace("\\", "/");
            pipeline.Status = AutoVideoPipelineStatus.Rendered;

            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(userId, pipelineId, true,
                "🎥 Render tamamlandı!");

            return finalPath;
        }
    }
}
