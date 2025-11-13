using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;

namespace Application.Services
{
    public class AutoVideoAssetService
    {
        private readonly IRepository<AutoVideoAsset> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public AutoVideoAssetService(
            IRepository<AutoVideoAsset> repo,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _repo = repo;
            _uow = uow;
            _current = current;
        }

        // ================================================================
        // 1) Yeni Asset oluştur
        // ================================================================
        public async Task<AutoVideoAsset> CreatePendingAsync(
            int profileId,
            CancellationToken ct = default)
        {
            var entity = new AutoVideoAsset
            {
                AppUserId = _current.UserId,
                ProfileId = profileId,
                Status = AutoVideoAssetStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return entity;
        }

        // ================================================================
        // 2) Topic bağla
        // ================================================================
        public async Task AttachTopicAsync(int assetId, int topicId, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.TopicId = topicId;
            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 3) Script bağla
        // ================================================================
        public async Task AttachScriptAsync(int assetId, int scriptId, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.ScriptId = scriptId;
            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 4) Status set
        // ================================================================
        public async Task UpdateStatusAsync(
            int assetId,
            AutoVideoAssetStatus status,
            CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.Status = status;
            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 5) VideoPath yaz
        // ================================================================
        public async Task SetVideoPathAsync(int assetId, string path, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.VideoPath = path;
            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 6) ThumbnailPath yaz
        // ================================================================
        public async Task SetThumbnailPathAsync(int assetId, string path, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.ThumbnailPath = path;
            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 7) Uploading
        // ================================================================
        public async Task MarkUploadingAsync(int assetId, string platform, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.Status = AutoVideoAssetStatus.Uploading;
            e.UploadPlatform = platform;

            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 8) Uploaded
        // ================================================================
        public async Task MarkUploadedAsync(int assetId, string videoId, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            e.Status = AutoVideoAssetStatus.Uploaded;
            e.Uploaded = true;
            e.UploadVideoId = videoId;

            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 9) Log Append
        // ================================================================
        public async Task AppendLogAsync(
            int assetId,
            string message,
            object? data,
            CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                throw new KeyNotFoundException("AutoVideoAsset bulunamadı.");

            var logEntry = new
            {
                at = DateTime.UtcNow,
                msg = message,
                data
            };

            string json = System.Text.Json.JsonSerializer.Serialize(logEntry);

            if (string.IsNullOrWhiteSpace(e.Log))
                e.Log = $"[{json}]";
            else
                e.Log = e.Log!.TrimEnd(']') + "," + json + "]";

            await _uow.SaveChangesAsync(ct);
        }

        // ================================================================
        // 10) Fail
        // ================================================================
        public async Task MarkFailedAsync(
            int assetId,
            string error,
            object? data,
            CancellationToken ct)
        {
            await AppendLogAsync(assetId, $"ERROR: {error}", data, ct);

            var e = await _repo.FirstOrDefaultAsync(
                x => x.Id == assetId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (e != null)
            {
                e.Status = AutoVideoAssetStatus.Failed;
                await _uow.SaveChangesAsync(ct);
            }
        }
    }
}
