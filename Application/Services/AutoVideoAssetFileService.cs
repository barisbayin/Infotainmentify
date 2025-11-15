using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;

namespace Application.Services
{
    public class AutoVideoAssetFileService
    {
        private readonly IRepository<AutoVideoAssetFile> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public AutoVideoAssetFileService(
            IRepository<AutoVideoAssetFile> repo,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _repo = repo;
            _uow = uow;
            _current = current;
        }

        // CREATE
        public async Task<AutoVideoAssetFile> CreateAsync(
            int pipelineId,
            int scene,
            string path,
            AutoVideoAssetFileType type,
            CancellationToken ct = default)
        {
            var file = new AutoVideoAssetFile
            {
                AppUserId = _current.UserId,
                AutoVideoPipelineId = pipelineId,
                SceneNumber = scene,
                FilePath = path,
                FileType = type,
                CreatedAt = DateTime.Now
            };

            await _repo.AddAsync(file, ct);
            await _uow.SaveChangesAsync(ct);
            return file;
        }

        // MULTI ADD
        public async Task AddMultipleAsync(
            int pipelineId,
            IEnumerable<(int Scene, string Path, AutoVideoAssetFileType Type)> items,
            CancellationToken ct = default)
        {
            var entities = items.Select(i => new AutoVideoAssetFile
            {
                AppUserId = _current.UserId,
                AutoVideoPipelineId = pipelineId,
                SceneNumber = i.Scene,
                FilePath = i.Path,
                FileType = i.Type,
                CreatedAt = DateTime.Now
            });

            await _repo.AddRangeAsync(entities, ct);
            await _uow.SaveChangesAsync(ct);
        }

        // READ: List by Pipeline
        public async Task<IReadOnlyList<AutoVideoAssetFile>> GetByPipelineAsync(int pipelineId, CancellationToken ct = default)
        {
            return await _repo.FindAsync(
                x => x.AutoVideoPipelineId == pipelineId && x.AppUserId == _current.UserId,
                asNoTracking: true,
                ct: ct
            );
        }

        // READ: Single
        public async Task<AutoVideoAssetFile?> GetAsync(int id, CancellationToken ct = default)
        {
            return await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct
            );
        }

        // DELETE one
        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await GetAsync(id, ct);
            if (entity == null) return false;

            _repo.Delete(entity);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // DELETE all for pipeline
        public async Task<int> DeleteByPipelineAsync(int pipelineId, CancellationToken ct = default)
        {
            var items = await _repo.FindAsync(
                x => x.AutoVideoPipelineId == pipelineId && x.AppUserId == _current.UserId,
                asNoTracking: false,
                ct: ct
            );

            foreach (var item in items)
                _repo.Delete(item);

            await _uow.SaveChangesAsync(ct);
            return items.Count;
        }

        // UPDATE path
        public async Task<bool> UpdatePathAsync(int id, string newPath, CancellationToken ct = default)
        {
            var entity = await GetAsync(id, ct);
            if (entity == null)
                return false;

            entity.FilePath = newPath;
            entity.UpdatedAt = DateTime.Now;

            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return true;
        }
    }
}
