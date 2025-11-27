using Application.Contracts.Script;
using Application.Mappers;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IReadOnlyList<ScriptGenerationProfileListDto>> ListAsync(
            int userId,
            string? status,
            CancellationToken ct)
        {
            var list = await _repo.FindAsync(
                p => p.AppUserId == userId &&
                     (string.IsNullOrWhiteSpace(status) || p.Status == status),
                asNoTracking: true,
                ct: ct,
                x => x.Prompt,
                x => x.AiConnection,
                x => x.TopicGenerationProfile,
                x => x.ImageAiConnection,
                x => x.TtsAiConnection,
                x => x.VideoAiConnection
            );

            return list.Select(x => x.ToListDto()).ToList();
        }

        // ---------------- GET ----------------
        public async Task<ScriptGenerationProfileDetailDto?> GetAsync(
            int userId,
            int id,
            CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                include: q => q
                    .Include(x => x.Prompt)
                    .Include(x => x.AiConnection)
                    .Include(x => x.TopicGenerationProfile)
                    .Include(x => x.ImageAiConnection)
                    .Include(x => x.TtsAiConnection)
                    .Include(x => x.VideoAiConnection),
                asNoTracking: true,
                ct: ct);

            return entity?.ToDetailDto();
        }

        // ---------------- UPSERT ----------------
        public async Task<int> UpsertAsync(
            int userId,
            ScriptGenerationProfileDetailDto dto,
            CancellationToken ct)
        {
            var profileName = dto.ProfileName.Trim();
            var modelName = dto.ModelName.Trim();

            // --- CREATE ---
            if (dto.Id == 0)
            {
                var exists = await _repo.AnyAsync(p =>
                    p.AppUserId == userId &&
                    p.PromptId == dto.PromptId &&
                    p.AiConnectionId == dto.AiConnectionId &&
                    p.ProfileName == profileName &&
                    p.ModelName == modelName, ct);

                if (exists)
                    throw new InvalidOperationException("Bu kombinasyonda bir profil zaten mevcut.");

                var e = new ScriptGenerationProfile
                {
                    AppUserId = userId,
                    PromptId = dto.PromptId,
                    AiConnectionId = dto.AiConnectionId,
                    TopicGenerationProfileId = dto.TopicGenerationProfileId ?? 0,
                    ProfileName = profileName,
                    ModelName = modelName,
                    Temperature = dto.Temperature,
                    Language = dto.Language,
                    OutputMode = dto.OutputMode ?? "Script",
                    ConfigJson = dto.ConfigJson ?? "{}",
                    Status = dto.Status ?? "Pending",
                    ProductionType = dto.ProductionType,
                    RenderStyle = dto.RenderStyle,
                    IsPublic = dto.IsPublic,
                    AllowRetry = dto.AllowRetry,

                    // 🎨 Image
                    ImageAiConnectionId = dto.ImageAiConnectionId,
                    ImageModelName = dto.ImageModelName,
                    ImageRenderStyle = dto.ImageRenderStyle,
                    ImageAspectRatio = dto.ImageAspectRatio ?? "16:9",

                    // 🗣️ TTS
                    TtsAiConnectionId = dto.TtsAiConnectionId,
                    TtsModelName = dto.TtsModelName,
                    TtsVoice = dto.TtsVoice,

                    SttAiConnectionId = dto.SttAiConnectionId,
                    SttModelName = dto.SttModelName,

                    // 🎬 Video
                    VideoAiConnectionId = dto.VideoAiConnectionId,
                    VideoModelName = dto.VideoModelName,
                    VideoTemplate = dto.VideoTemplate,

                    // 🔄 Auto Flags
                    AutoGenerateAssets = dto.AutoGenerateAssets,
                    AutoRenderVideo = dto.AutoRenderVideo
                };

                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }

            // --- UPDATE ---
            var entity = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == dto.Id,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                throw new KeyNotFoundException("ScriptGenerationProfile bulunamadı.");

            entity.PromptId = dto.PromptId;
            entity.AiConnectionId = dto.AiConnectionId;
            entity.TopicGenerationProfileId = dto.TopicGenerationProfileId ?? 0;
            entity.ProfileName = profileName;
            entity.ModelName = modelName;
            entity.Temperature = dto.Temperature;
            entity.Language = dto.Language;
            entity.OutputMode = dto.OutputMode ?? "Script";
            entity.ConfigJson = dto.ConfigJson ?? "{}";
            entity.Status = dto.Status ?? entity.Status;
            entity.ProductionType = dto.ProductionType;
            entity.RenderStyle = dto.RenderStyle;
            entity.IsPublic = dto.IsPublic;
            entity.AllowRetry = dto.AllowRetry;

            // 🎨 Image
            entity.ImageAiConnectionId = dto.ImageAiConnectionId;
            entity.ImageModelName = dto.ImageModelName;
            entity.ImageRenderStyle = dto.ImageRenderStyle;
            entity.ImageAspectRatio = dto.ImageAspectRatio ?? "16:9";

            // 🗣️ TTS
            entity.TtsAiConnectionId = dto.TtsAiConnectionId;
            entity.TtsModelName = dto.TtsModelName;
            entity.TtsVoice = dto.TtsVoice;

            entity.SttAiConnectionId = dto.SttAiConnectionId;
            entity.SttModelName = dto.SttModelName;

            // 🎬 Video
            entity.VideoAiConnectionId = dto.VideoAiConnectionId;
            entity.VideoModelName = dto.VideoModelName;
            entity.VideoTemplate = dto.VideoTemplate;

            // 🔄 Auto Flags
            entity.AutoGenerateAssets = dto.AutoGenerateAssets;
            entity.AutoRenderVideo = dto.AutoRenderVideo;

            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);
            return entity.Id;
        }

        // ---------------- DELETE ----------------
        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                asNoTracking: false,
                ct: ct);

            if (e is null)
                return false;

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
