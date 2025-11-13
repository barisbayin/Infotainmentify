using Application.Contracts.VideoAsset;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    /// <summary>
    /// Script'lere ait üretilmiş video, görsel, ses gibi asset kayıtlarını yönetir.
    /// </summary>
    public class VideoAssetService
    {
        private readonly IRepository<VideoAsset> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public VideoAssetService(
            IRepository<VideoAsset> repo,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _repo = repo;
            _uow = uow;
            _current = current;
        }

        // ---------------- LIST ----------------
        public async Task<IReadOnlyList<VideoAssetListDto>> ListAsync(
            int? scriptId = null,
            string? assetType = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default)
        {
            var userId = _current.UserId;

            var items = await _repo.FindAsync(
                predicate: x =>
                    x.UserId == userId &&
                    (!scriptId.HasValue || x.ScriptId == scriptId.Value) &&
                    (string.IsNullOrEmpty(assetType) || x.AssetType == assetType) &&
                    (!from.HasValue || x.GeneratedAt >= from.Value) &&
                    (!to.HasValue || x.GeneratedAt <= to.Value),
                orderBy: x => x.GeneratedAt ?? x.CreatedAt,
                desc: true,
                asNoTracking: true,
                ct: ct);

            return items.Select(VideoAssetMapper.ToListDto).ToList();
        }

        // ---------------- DETAILS ----------------
        public async Task<VideoAssetDetailDto?> GetAsync(int id, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, ct);
            if (entity == null || entity.UserId != _current.UserId)
                return null;

            return VideoAssetMapper.ToDetailDto(entity);
        }

        // ---------------- DELETE ----------------
        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(id, false, ct);
            if (entity == null || entity.UserId != _current.UserId)
                return false;

            _repo.Delete(entity);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
