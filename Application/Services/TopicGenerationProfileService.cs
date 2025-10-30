using Application.Contracts.TopicGenerationProfile;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;


namespace Application.Services
{
    public class TopicGenerationProfileService
    {
        private readonly IRepository<TopicGenerationProfile> _repo;
        private readonly IUnitOfWork _uow;

        public TopicGenerationProfileService(IRepository<TopicGenerationProfile> repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }
        public Task<IReadOnlyList<TopicGenerationProfile>> ListAsync(
            int userId,
            string? status,
            CancellationToken ct)
            => _repo.FindAsync(
                p => p.AppUserId == userId &&
                     (string.IsNullOrWhiteSpace(status) || p.Status == status),
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection);

        // -------------------- Tek kayıt --------------------
        public Task<TopicGenerationProfile?> GetAsync(
            int userId,
            int id,
            CancellationToken ct)
            => _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection);


        /// <summary>
        /// Id == 0 → Create, Id > 0 → Update.
        /// (UserId, PromptId, AiConnectionId, ModelName) benzersiz kontrolü içerir.
        /// </summary>
        public async Task<int> UpsertAsync(int userId, TopicGenerationProfileDetailDto dto, CancellationToken ct)
        {
            var modelName = dto.ModelName.Trim();

            if (dto.Id == 0)
            {
                // UNIQUE: (UserId, PromptId, AiConnectionId, ModelName)
                var exists = await _repo.AnyAsync(p =>
                    p.AppUserId == userId &&
                    p.PromptId == dto.PromptId &&
                    p.AiConnectionId == dto.AiConnectionId &&
                    p.ModelName == modelName, ct);

                if (exists)
                    throw new InvalidOperationException("Bu kombinasyonda bir profil zaten mevcut.");

                var e = new TopicGenerationProfile
                {
                    AppUserId = userId,
                    PromptId = dto.PromptId,
                    AiConnectionId = dto.AiConnectionId,
                    ModelName = modelName,
                    RequestedCount = dto.RequestedCount,
                    RawResponseJson = dto.RawResponseJson ?? "{}",
                    StartedAt = dto.StartedAt ?? DateTimeOffset.UtcNow,
                    CompletedAt = dto.CompletedAt,
                    Status = dto.Status ?? "Pending"
                };

                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
            else
            {
                var e = await _repo.FirstOrDefaultAsync(
                    p => p.AppUserId == userId && p.Id == dto.Id,
                    asNoTracking: false, ct: ct);

                if (e is null)
                    throw new KeyNotFoundException("TopicGenerationProfile bulunamadı.");

                // uniqueness check if AiConnection/Prompt/ModelName changed
                if (e.PromptId != dto.PromptId ||
                    e.AiConnectionId != dto.AiConnectionId ||
                    !string.Equals(e.ModelName, modelName, StringComparison.Ordinal))
                {
                    var clash = await _repo.AnyAsync(p =>
                        p.AppUserId == userId &&
                        p.PromptId == dto.PromptId &&
                        p.AiConnectionId == dto.AiConnectionId &&
                        p.ModelName == modelName &&
                        p.Id != dto.Id, ct);

                    if (clash)
                        throw new InvalidOperationException("Bu kombinasyonda başka bir profil zaten mevcut.");
                }

                e.PromptId = dto.PromptId;
                e.AiConnectionId = dto.AiConnectionId;
                e.ModelName = modelName;
                e.RequestedCount = dto.RequestedCount;
                e.RawResponseJson = dto.RawResponseJson ?? "{}";
                e.StartedAt = dto.StartedAt ?? e.StartedAt;
                e.CompletedAt = dto.CompletedAt;
                e.Status = dto.Status ?? e.Status;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(p => p.AppUserId == userId && p.Id == id, asNoTracking: false, ct: ct);
            if (e is null) return false;

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
