using Application.Abstractions;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Entity.User;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    public class AutoVideoPipelineService
    {
        private readonly IRepository<ContentPipelineRun_> _pipelineRepo;
        private readonly IRepository<VideoGenerationProfile> _profileRepo;
        private readonly IRepository<AutoVideoAssetFile> _assetRepo;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly IUserDirectoryService _dir;
        private readonly INotifierService _notifier;

        public AutoVideoPipelineService(
            IRepository<ContentPipelineRun_> pipelineRepo,
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
        public async Task<ContentPipelineRun_> CreatePipelineAsync(int userId, int profileId, CancellationToken ct)
        {
            try
            {
                var appUser = await _userRepo.GetByIdAsync(userId);

                ct.ThrowIfCancellationRequested();

                // Profil user'a mı ait?
                var profile = await _profileRepo.GetByIdAsync(profileId, asNoTracking: true, ct);
                if (profile == null || profile.AppUserId != userId)
                    throw new UnauthorizedAccessException("Profile erişilemedi.");

                // Pipeline kaydı oluştur
                var pipeline = new ContentPipelineRun_
                {
                    AppUserId = userId,
                    ProfileId = profileId,
                    Status = ContentPipelineStatus.Pending,
                    CreatedAt = DateTime.Now,
                    LogJson = "[]"
                };

                await _pipelineRepo.AddAsync(pipeline, ct);
                await _uow.SaveChangesAsync(ct);

                // Klasörleri oluştur
                await CreatePipelineFolders(pipeline, appUser, ct);

                // İlk log
                AppendLog(pipeline, "Pipeline oluşturuldu.");
                await _uow.SaveChangesAsync(ct);

                return pipeline;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreatePipelineAsync: {ex.Message}");   
                throw;
            }   
        }

        // ------------------------------------------------------------
        // GET PIPELINE DETAILS
        // ------------------------------------------------------------
        public async Task<ContentPipelineRun_?> GetAsync(int id, CancellationToken ct)
        {
            return await _pipelineRepo.FirstOrDefaultAsync(
                x => x.Id == id && x.AppUserId == _current.UserId,
                include: q => q
                    .Include(x => x.Profile)
                        .ThenInclude(p => p.AutoVideoRenderProfile)
                    .Include(x => x.Profile)
                        .ThenInclude(p => p.ScriptGenerationProfile)
                    .Include(x => x.Profile)
                    .Include(x => x.Script),
                asNoTracking: true,
                ct: ct
            );

        }

        // ------------------------------------------------------------
        // LIST PIPELINES (USER SCOPED)
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<ContentPipelineRun_>> ListAsync(CancellationToken ct)
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
        public async Task UpdateStatusAsync(int pipelineId, int? userId, ContentPipelineStatus status, CancellationToken ct)
        {
            if (!userId.HasValue)
            {
                userId = _current.UserId;
            }

            var p = await _pipelineRepo.GetByIdAsync(pipelineId, asNoTracking: false, ct);
            if (p == null || p.AppUserId != userId)
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
        private void AppendLog(ContentPipelineRun_ pipeline, string message)
        {
            var logs = !string.IsNullOrWhiteSpace(pipeline.LogJson)
                ? JsonSerializer.Deserialize<List<string>>(pipeline.LogJson, Utf8JsonOptions) ?? new List<string>()
                : new List<string>();

            logs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

            pipeline.LogJson = JsonSerializer.Serialize(logs, Utf8JsonOptions);
        }

        // ------------------------------------------------------------
        // HELPER: Add log + save
        // ------------------------------------------------------------
        public async Task AddLogAsync(ContentPipelineRun_ p, string msg, CancellationToken ct)
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
        private async Task CreatePipelineFolders(ContentPipelineRun_ pipeline, AppUser appUser, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Kullanıcı klasörü zaten var mı? yoksa oluştur
            await _dir.EnsureUserScaffoldAsync(appUser!, ct);

            var root = _dir.GetVideoPipelineRoot(appUser!, pipeline.Id);
            Directory.CreateDirectory(root);

            Directory.CreateDirectory(Path.Combine(root, "assets"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "images"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "audio"));
            Directory.CreateDirectory(Path.Combine(root, "assets", "raw"));

            Directory.CreateDirectory(Path.Combine(root, "render"));
            Directory.CreateDirectory(Path.Combine(root, "render", "scenes"));
            Directory.CreateDirectory(Path.Combine(root, "render", "final"));

            //Directory.CreateDirectory(Path.Combine(root, "final"));
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

        public static readonly JsonSerializerOptions Utf8JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };
    }
}
