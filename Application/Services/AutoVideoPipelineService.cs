using Application.Abstractions;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class AutoVideoPipelineService
    {
        private readonly IRepository<AutoVideoPipeline> _pipelineRepo;
        private readonly IRepository<VideoGenerationProfile> _profileRepo;
        private readonly IRepository<AutoVideoAssetFile> _assetRepo;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly IUserDirectoryService _dir;
        private readonly INotifierService _notifier;

        public AutoVideoPipelineService(
            IRepository<AutoVideoPipeline> pipelineRepo,
            IRepository<VideoGenerationProfile> profileRepo,
            IRepository<AutoVideoAssetFile> assetRepo,
            IRepository<AppUser> userRepo,
            IUnitOfWork uow,
            ICurrentUserService current,
            IUserDirectoryService dir,
            INotifierService notifier)
        {
            _pipelineRepo = pipelineRepo;
            _profileRepo = profileRepo;
            _assetRepo = assetRepo;
            _userRepo = userRepo;
            _uow = uow;
            _current = current;
            _dir = dir;
            _notifier = notifier;
        }

        // ------------------------------------------------------------
        // CREATE PIPELINE
        // ------------------------------------------------------------
        public async Task<AutoVideoPipeline> CreatePipelineAsync(int profileId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Profil user'a mı ait?
            var profile = await _profileRepo.GetByIdAsync(profileId, asNoTracking: true, ct);
            if (profile == null || profile.AppUserId != _current.UserId)
                throw new UnauthorizedAccessException("Profile erişilemedi.");

            // Pipeline kaydı oluştur
            var pipeline = new AutoVideoPipeline
            {
                AppUserId = _current.UserId,
                ProfileId = profileId,
                Status = AutoVideoPipelineStatus.Pending,
                CreatedAt = DateTime.Now,
                LogJson = "[]"
            };

            await _pipelineRepo.AddAsync(pipeline, ct);
            await _uow.SaveChangesAsync(ct);

            // Klasörleri oluştur
            await CreatePipelineFolders(pipeline, ct);

            // İlk log
            AppendLog(pipeline, "Pipeline oluşturuldu.");
            await _uow.SaveChangesAsync(ct);

            return pipeline;
        }

        // ------------------------------------------------------------
        // GET PIPELINE DETAILS
        // ------------------------------------------------------------
        public async Task<AutoVideoPipeline?> GetAsync(int id, CancellationToken ct)
        {
            return await _pipelineRepo.FirstOrDefaultAsync(
                x => x.Id == id && x.AppUserId == _current.UserId,
                include: q => q
                    .Include(x => x.Profile)
                    .Include(x => x.Topic)
                    .Include(x => x.Script),
                asNoTracking: true,
                ct: ct
            );
        }

        // ------------------------------------------------------------
        // LIST PIPELINES (USER SCOPED)
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<AutoVideoPipeline>> ListAsync(CancellationToken ct)
        {
            return await _pipelineRepo.FindAsync(
                x => x.AppUserId == _current.UserId,
                include: q => q
                    .Include(x => x.Profile)
                    .Include(x => x.Topic)
                    .Include(x => x.Script),
                asNoTracking: true,
                ct: ct
            );
        }

        // ------------------------------------------------------------
        // UPDATE STATUS
        // ------------------------------------------------------------
        public async Task UpdateStatusAsync(int pipelineId, AutoVideoPipelineStatus status, CancellationToken ct)
        {
            var p = await _pipelineRepo.GetByIdAsync(pipelineId, asNoTracking: false, ct);
            if (p == null || p.AppUserId != _current.UserId)
                throw new UnauthorizedAccessException("Pipeline bulunamadı veya size ait değil.");

            p.Status = status;
            p.UpdatedAt = DateTime.Now;

            AppendLog(p, $"Status update: {status}");

            await _uow.SaveChangesAsync(ct);

            // SignalR notify
            await _notifier.NotifyUserAsync(
                _current.UserId,
                "pipeline.status",
                new
                {
                    pipelineId,
                    status = status.ToString()
                }
            );
        }

        // ------------------------------------------------------------
        // APPEND LOG
        // ------------------------------------------------------------
        public void AppendLog(AutoVideoPipeline pipeline, string msg)
        {
            var logs = string.IsNullOrWhiteSpace(pipeline.LogJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(pipeline.LogJson!)!;

            logs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}");

            pipeline.LogJson = System.Text.Json.JsonSerializer.Serialize(logs);
        }

        // ------------------------------------------------------------
        // HELPER: Add log + save
        // ------------------------------------------------------------
        public async Task AddLogAsync(AutoVideoPipeline p, string msg, CancellationToken ct)
        {
            AppendLog(p, msg);
            await _uow.SaveChangesAsync(ct);

            await _notifier.NotifyUserAsync(
                _current.UserId,
                "pipeline.log",
                new { pipelineId = p.Id, message = msg }
            );
        }

        // ------------------------------------------------------------
        // CREATE PIPELINE FOLDERS
        // ------------------------------------------------------------
        private async Task CreatePipelineFolders(AutoVideoPipeline pipeline, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var user = await _userRepo.GetByIdAsync(_current.UserId, asNoTracking: true, ct);

            // Kullanıcı klasörü zaten var mı? yoksa oluştur
            await _dir.EnsureUserScaffoldAsync(user!, ct);

            var root = _dir.GetVideoPipelineRoot(user!, pipeline.Id);
            Directory.CreateDirectory(root);

            Directory.CreateDirectory(Path.Combine(root, "assets"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "images"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "audio"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "raw"));

            Directory.CreateDirectory(Path.Combine(root, "render"));
            Directory.CreateDirectory(Path.Combine(root, "render", "scenes"));
            Directory.CreateDirectory(Path.Combine(root, "render", "merged"));

            Directory.CreateDirectory(Path.Combine(root, "final"));
            Directory.CreateDirectory(Path.Combine(root, "temp"));
        }

        // ------------------------------------------------------------
        // PATH HELPERS FOR VIDEO GENERATION SERVICE
        // ------------------------------------------------------------
        public async Task<string> GetFinalVideoPathAsync(int pipelineId, CancellationToken ct)
        {
            var user = await _userRepo.GetByIdAsync(_current.UserId, true, ct);
            return Path.Combine(_dir.GetPipelineFinalRoot(user!, pipelineId), "video.mp4");
        }

        public async Task<string> GetThumbnailPathAsync(int pipelineId, CancellationToken ct)
        {
            var user = await _userRepo.GetByIdAsync(_current.UserId, true, ct);
            return Path.Combine(_dir.GetPipelineFinalRoot(user!, pipelineId), "thumbnail.jpg");
        }
    }
}
