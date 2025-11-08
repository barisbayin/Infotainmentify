using Application.Contracts.Mappers;
using Application.Contracts.Topics;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class TopicService
    {
        private readonly IRepository<Topic> _repo;
        private readonly IUnitOfWork _uow;

        public TopicService(IRepository<Topic> repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        // ------------------- LIST -------------------
        public async Task<IReadOnlyList<TopicListDto>> ListAsync(
            int userId,
            string? q,
            string? category,
            CancellationToken ct)
        {
            var list = await _repo.FindAsync(
                predicate: t =>
                    t.UserId == userId &&
                    (string.IsNullOrWhiteSpace(q)
                        || (t.Premise != null && t.Premise.Contains(q))
                        || (t.PremiseTr != null && t.PremiseTr.Contains(q))) &&
                    (string.IsNullOrWhiteSpace(category) || t.Category == category),
                orderBy: t => t.Id,
                desc: true,
                include: q => q
                    .Include(t => t.Prompt)
                    .Include(t => t.Script),
                asNoTracking: true,
                ct: ct
            );

            return list.Select(t => t.ToListDto()).ToList();
        }

        // ------------------- GET -------------------
        public async Task<TopicDetailDto?> GetAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                t => t.UserId == userId && t.Id == id,
                include: q => q.Include(t => t.Prompt).Include(t => t.Script),
                asNoTracking: true,
                ct: ct);

            return e?.ToDetailDto();
        }

        // ------------------- UPSERT -------------------
        public async Task<int> UpsertAsync(int userId, TopicDetailDto dto, CancellationToken ct)
        {
            var code = dto.TopicJson?.Trim();

            if (dto.Id == 0)
            {
                // Benzersizlik kontrolü
                if (!string.IsNullOrEmpty(code))
                {
                    var exists = await _repo.AnyAsync(
                        t => t.UserId == userId && t.TopicCode == code, ct);
                    if (exists)
                        throw new InvalidOperationException("Bu TopicCode zaten kullanılıyor.");
                }

                var e = new Topic
                {
                    UserId = userId,
                    TopicCode = code ?? $"topic-{DateTime.Now:yyyyMMdd-HHmmss}",
                    Category = dto.Category?.Trim(),
                    SubCategory = dto.SubCategory?.Trim(),
                    Series = dto.Series,
                    Premise = dto.Premise,
                    PremiseTr = dto.PremiseTr,
                    Tone = dto.Tone,
                    PotentialVisual = dto.PotentialVisual,
                    RenderStyle = dto.RenderStyle,
                    VoiceHint = dto.VoiceHint,
                    ScriptHint = dto.ScriptHint,
                    FactCheck = dto.FactCheck,
                    NeedsFootage = dto.NeedsFootage,
                    Priority = dto.Priority,
                    TopicJson = dto.TopicJson,
                    PromptId = dto.PromptId,
                    ScriptId = dto.ScriptId,
                    ScriptGenerated = dto.ScriptGenerated,
                    ScriptGeneratedAt = dto.ScriptGeneratedAt,
                    IsActive = dto.IsActive
                };

                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
            else
            {
                var e = await _repo.FirstOrDefaultAsync(
                    t => t.UserId == userId && t.Id == dto.Id,
                    asNoTracking: false, ct: ct);

                if (e is null)
                    throw new KeyNotFoundException("Topic bulunamadı.");

                // Benzersizlik
                if (!string.Equals(e.TopicCode, code, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(code))
                {
                    var clash = await _repo.AnyAsync(
                        t => t.UserId == userId && t.TopicCode == code && t.Id != dto.Id, ct);
                    if (clash)
                        throw new InvalidOperationException("Bu TopicCode zaten kullanılıyor.");
                }

                e.TopicCode = code ?? e.TopicCode;
                e.Category = dto.Category?.Trim();
                e.SubCategory = dto.SubCategory?.Trim();
                e.Series = dto.Series;
                e.Premise = dto.Premise;
                e.PremiseTr = dto.PremiseTr;
                e.Tone = dto.Tone;
                e.PotentialVisual = dto.PotentialVisual;
                e.RenderStyle = dto.RenderStyle;
                e.VoiceHint = dto.VoiceHint;
                e.ScriptHint = dto.ScriptHint;
                e.FactCheck = dto.FactCheck;
                e.NeedsFootage = dto.NeedsFootage;
                e.Priority = dto.Priority;
                e.TopicJson = dto.TopicJson;
                e.PromptId = dto.PromptId;
                e.ScriptId = dto.ScriptId;
                e.ScriptGenerated = dto.ScriptGenerated;
                e.ScriptGeneratedAt = dto.ScriptGeneratedAt;
                e.IsActive = dto.IsActive;
                e.UpdatedAt = DateTime.Now;

                _repo.Update(e);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }
        }

        // ------------------- DELETE -------------------
        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                t => t.UserId == userId && t.Id == id,
                asNoTracking: false, ct: ct);

            if (e is null)
                return false;

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // ------------------- TOGGLE ACTIVE -------------------
        public async Task ToggleActiveAsync(int userId, int id, bool isActive, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                t => t.UserId == userId && t.Id == id,
                asNoTracking: false, ct: ct);

            if (e == null)
                throw new KeyNotFoundException($"Topic #{id} bulunamadı.");

            e.IsActive = isActive;
            e.UpdatedAt = DateTime.Now;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task ToggleAllowScriptGenerationAsync(int userId, int topicId, bool allow, CancellationToken ct)
        {
            var topic = await _repo.FirstOrDefaultAsync(
                x => x.UserId == userId && x.Id == topicId,
                asNoTracking: false, ct: ct);

            if (topic is null)
                throw new KeyNotFoundException("Topic bulunamadı.");

            topic.AllowScriptGeneration = allow;
            topic.UpdatedAt = DateTime.Now;

            _repo.Update(topic);
            await _uow.SaveChangesAsync(ct);
        }

    }
}