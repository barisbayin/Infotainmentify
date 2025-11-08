using Application.Contracts.Topics;
using Application.Mappers;
using Core.Contracts;
using Core.Entity;

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

        // -------------------- LIST --------------------
        public async Task<IReadOnlyList<TopicGenerationProfileListDto>> ListAsync(
            int userId,
            string? q,
            CancellationToken ct)
        {
            var list = await _repo.FindAsync(
                p => p.AppUserId == userId &&
                     (string.IsNullOrWhiteSpace(q) ||
                      p.ProfileName.Contains(q) ||
                      p.ModelName.Contains(q) ||
                      (p.ProductionType ?? "").Contains(q) ||
                      (p.RenderStyle ?? "").Contains(q)),
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection);

            return list.Select(x => x.ToListDto()).ToList();
        }

        // -------------------- GET SINGLE --------------------
        public async Task<TopicGenerationProfileDetailDto?> GetAsync(
            int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection);

            return e?.ToDetailDto();
        }

        // -------------------- CREATE / UPDATE --------------------
        /// <summary>
        /// Id == 0 → Create, Id > 0 → Update.
        /// UNIQUE: (AppUserId, PromptId, AiConnectionId, ProfileName, ModelName)
        /// </summary>
        public async Task<int> UpsertAsync(int userId, TopicGenerationProfileDetailDto dto, CancellationToken ct)
        {
            var modelName = dto.ModelName.Trim();
            var profileName = dto.ProfileName.Trim();

            if (dto.Id == 0)
            {
                // Benzersizlik kontrolü
                var exists = await _repo.AnyAsync(p =>
                    p.AppUserId == userId &&
                    p.PromptId == dto.PromptId &&
                    p.AiConnectionId == dto.AiConnectionId &&
                    p.ProfileName == profileName &&
                    p.ModelName == modelName, ct);

                if (exists)
                    throw new InvalidOperationException("Bu kombinasyonda bir profil zaten mevcut.");

                var e = new TopicGenerationProfile
                {
                    AppUserId = userId,
                    PromptId = dto.PromptId,
                    AiConnectionId = dto.AiConnectionId,
                    ProfileName = profileName,
                    ModelName = modelName,
                    RequestedCount = dto.RequestedCount,
                    Temperature = dto.Temperature,
                    Language = dto.Language,
                    MaxTokens = dto.MaxTokens,
                    ProductionType = dto.ProductionType,
                    RenderStyle = dto.RenderStyle,
                    TagsJson = dto.TagsJson,
                    OutputMode = dto.OutputMode,
                    AutoGenerateScript = dto.AutoGenerateScript,
                    IsPublic = dto.IsPublic,
                    AllowRetry = dto.AllowRetry
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

                // Eğer benzersizlik alanları değiştiyse tekrar kontrol et
                if (e.PromptId != dto.PromptId ||
                    e.AiConnectionId != dto.AiConnectionId ||
                    !string.Equals(e.ModelName, modelName, StringComparison.Ordinal) ||
                    !string.Equals(e.ProfileName, profileName, StringComparison.Ordinal))
                {
                    var clash = await _repo.AnyAsync(p =>
                        p.AppUserId == userId &&
                        p.PromptId == dto.PromptId &&
                        p.AiConnectionId == dto.AiConnectionId &&
                        p.ProfileName == profileName &&
                        p.ModelName == modelName &&
                        p.Id != dto.Id, ct);

                    if (clash)
                        throw new InvalidOperationException("Bu kombinasyonda başka bir profil zaten mevcut.");
                }

                // Güncelle
                e.ProfileName = profileName;
                e.PromptId = dto.PromptId;
                e.AiConnectionId = dto.AiConnectionId;
                e.ModelName = modelName;
                e.RequestedCount = dto.RequestedCount;
                e.Temperature = dto.Temperature;
                e.Language = dto.Language;
                e.MaxTokens = dto.MaxTokens;
                e.ProductionType = dto.ProductionType;
                e.RenderStyle = dto.RenderStyle;
                e.TagsJson = dto.TagsJson;
                e.OutputMode = dto.OutputMode;
                e.AutoGenerateScript = dto.AutoGenerateScript;
                e.IsPublic = dto.IsPublic;
                e.AllowRetry = dto.AllowRetry;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }

        // -------------------- DELETE --------------------
        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                asNoTracking: false, ct: ct);

            if (e is null)
                return false;

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
