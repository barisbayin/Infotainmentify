using Application.Contracts.Script;
using Application.Mappers;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    public class ScriptGenerationProfileService
    {
        private readonly IRepository<ScriptGenerationProfile> _repo;
        private readonly IUnitOfWork _uow;

        public ScriptGenerationProfileService(IRepository<ScriptGenerationProfile> repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        // ---------------- LIST ----------------
        public async Task<IReadOnlyList<ScriptGenerationProfileListDto>> ListAsync(int userId, string? status, CancellationToken ct)
        {
            var list = await _repo.FindAsync(
                p => p.AppUserId == userId &&
                     (string.IsNullOrWhiteSpace(status) || p.Status == status),
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection,
                x => x.TopicGenerationProfile);

            return list.Select(x => x.ToListDto()).ToList();
        }

        // ---------------- GET ----------------
        public async Task<ScriptGenerationProfileDetailDto?> GetAsync(int userId, int id, CancellationToken ct)
        {
            var q = await _repo.FindAsync(
                p => p.AppUserId == userId && p.Id == id,
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection,
                x => x.TopicGenerationProfile);

            return q.FirstOrDefault()?.ToDetailsDto();
        }

        // ---------------- UPSERT ----------------
        public async Task<int> UpsertAsync(int userId, ScriptGenerationProfileDetailDto dto, CancellationToken ct)
        {
            var modelName = dto.ModelName.Trim();
            var profileName = dto.ProfileName.Trim();

            if (dto.Id == 0)
            {
                var exists = await _repo.AnyAsync(p =>
                    p.AppUserId == userId &&
                    p.PromptId == dto.PromptId &&
                    p.AiConnectionId == dto.AiConnectionId &&
                    p.ProfileName == profileName &&
                    p.ModelName == modelName, ct);

                if (exists)
                    throw new InvalidOperationException("Bu kombinasyonda bir ScriptGenerationProfile zaten mevcut.");

                var e = new ScriptGenerationProfile
                {
                    AppUserId = userId,
                    PromptId = dto.PromptId,
                    AiConnectionId = dto.AiConnectionId,
                    TopicGenerationProfileId = dto.TopicGenerationProfileId,
                    ProfileName = profileName,
                    ModelName = modelName,
                    Temperature = dto.Temperature,
                    Language = dto.Language,
                    TopicIdsJson = dto.TopicIdsJson ?? "[]",
                    ConfigJson = dto.ConfigJson ?? "{}",
                    RawResponseJson = dto.RawResponseJson ?? "{}",
                    StartedAt = dto.StartedAt ?? DateTimeOffset.Now,
                    CompletedAt = dto.CompletedAt,
                    Status = dto.Status ?? "Pending",
                    IsActive = dto.IsActive
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

                if (e == null)
                    throw new KeyNotFoundException("ScriptGenerationProfile bulunamadı.");

                e.PromptId = dto.PromptId;
                e.AiConnectionId = dto.AiConnectionId;
                e.TopicGenerationProfileId = dto.TopicGenerationProfileId;
                e.ProfileName = profileName;
                e.ModelName = modelName;
                e.Temperature = dto.Temperature;
                e.Language = dto.Language;
                e.TopicIdsJson = dto.TopicIdsJson ?? "[]";
                e.ConfigJson = dto.ConfigJson ?? "{}";
                e.RawResponseJson = dto.RawResponseJson ?? "{}";
                e.StartedAt = dto.StartedAt ?? e.StartedAt;
                e.CompletedAt = dto.CompletedAt;
                e.Status = dto.Status ?? e.Status;
                e.IsActive = dto.IsActive;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }

        // ---------------- DELETE ----------------
        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(p => p.AppUserId == userId && p.Id == id, asNoTracking: false, ct: ct);
            if (e is null)
                return false;

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
