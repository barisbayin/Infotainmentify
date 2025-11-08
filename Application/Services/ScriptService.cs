using Application.Contracts.Script;
using Application.Contracts.Story;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Services
{
    public class ScriptService
    {
        private readonly IRepository<Script> _repo;
        private readonly IRepository<Topic> _topicRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public ScriptService(
            IRepository<Script> repo,
            IRepository<Topic> topicRepo,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _repo = repo;
            _topicRepo = topicRepo;
            _uow = uow;
            _current = current;
        }

        // ---------------- LIST ----------------
        public async Task<IReadOnlyList<ScriptListDto>> ListAsync(
            int? topicId = null,
            string? q = null,
            CancellationToken ct = default)
        {
            Expression<Func<Script, bool>> predicate = x => x.UserId == _current.UserId;

            if (topicId.HasValue)
                predicate = x => x.UserId == _current.UserId && x.TopicId == topicId.Value;

            var includes = new Expression<Func<Script, object>>[]
            {
                x => x.Topic,
                x => x.Prompt,
                x => x.AiConnection,
                x => x.ScriptGenerationProfile
            };

            var list = await _repo.FindAsync(
                predicate,
                orderBy: x => x.CreatedAt,
                desc: true,
                asNoTracking: true,
                ct,
                includes);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.ToLowerInvariant();
                list = list.Where(x =>
                    (x.Title != null && x.Title.ToLower().Contains(q)) ||
                    (x.Summary != null && x.Summary.ToLower().Contains(q)) ||
                    (x.Topic?.Premise != null && x.Topic.Premise.ToLower().Contains(q)))
                    .ToList();
            }

            return list.Select(x => x.ToListDto()).ToList();
        }

        // ---------------- GET ----------------
        public async Task<ScriptDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == _current.UserId,
                include: q => q
                    .Include(x => x.Topic)
                    .Include(x => x.Prompt)
                    .Include(x => x.AiConnection)
                    .Include(x => x.ScriptGenerationProfile),
                asNoTracking: true,
                ct: ct);

            return entity?.ToDetailDto();
        }

        // ---------------- CREATE ----------------
        public async Task<ScriptDetailDto> CreateAsync(ScriptDetailDto dto, CancellationToken ct)
        {
            var topicExists = await _topicRepo.AnyAsync(
                x => x.Id == dto.TopicId && x.UserId == _current.UserId, ct);

            if (!topicExists)
                throw new InvalidOperationException("Topic not found or unauthorized.");

            var entity = new Script
            {
                UserId = _current.UserId,
                TopicId = dto.TopicId,
                Title = dto.Title,
                Content = dto.Content,
                Summary = dto.Summary,
                Language = dto.Language,
                RenderStyle = dto.RenderStyle,
                ProductionType = dto.ProductionType,
                MetaJson = dto.MetaJson,
                ScriptJson = dto.ScriptJson,
                ResponseTimeMs = dto.ResponseTimeMs,
                PromptId = dto.PromptId,
                AiConnectionId = dto.AiConnectionId,
                ScriptGenerationProfileId = dto.ScriptGenerationProfileId
            };

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return entity.ToDetailDto();
        }

        // ---------------- UPDATE ----------------
        public async Task<ScriptDetailDto> UpdateAsync(int id, ScriptDetailDto dto, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                throw new InvalidOperationException("Script not found or unauthorized.");

            entity.Title = dto.Title;
            entity.Content = dto.Content;
            entity.Summary = dto.Summary;
            entity.Language = dto.Language;
            entity.RenderStyle = dto.RenderStyle;
            entity.ProductionType = dto.ProductionType;
            entity.MetaJson = dto.MetaJson;
            entity.ScriptJson = dto.ScriptJson;
            entity.ResponseTimeMs = dto.ResponseTimeMs;
            entity.PromptId = dto.PromptId;
            entity.AiConnectionId = dto.AiConnectionId;
            entity.ScriptGenerationProfileId = dto.ScriptGenerationProfileId;

            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return entity.ToDetailDto();
        }

        // ---------------- DELETE ----------------
        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == _current.UserId,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                throw new InvalidOperationException("Script not found or unauthorized.");

            _repo.Delete(entity);
            await _uow.SaveChangesAsync(ct);
        }
    }   
}
