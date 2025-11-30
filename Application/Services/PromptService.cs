using Application.Contracts.Prompts;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    public class PromptService : BaseService<Prompt>
    {
        public PromptService(IRepository<Prompt> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }

        // BaseService'de olmayan ÖZEL ARAMA
        public async Task<IReadOnlyList<Prompt>> ListAsync(
            int userId, string? q, string? category, bool? active, CancellationToken ct)
        {
            return await _repo.FindAsync(p =>
                   p.UserId == userId &&
                   (string.IsNullOrWhiteSpace(q) ||
                        p.Name.Contains(q) ||
                        p.Body.Contains(q) ||
                        (p.Description != null && p.Description.Contains(q))) &&
                   (string.IsNullOrWhiteSpace(category) || p.Category == category) &&
                   (!active.HasValue || p.IsActive == active),
               asNoTracking: true, ct);
        }

        // CREATE (Özel Validasyonlu)
        public async Task<int> CreateAsync(SavePromptDto dto, int userId, CancellationToken ct)
        {
            var name = dto.Name.Trim();

            // Kural: Aynı kullanıcının aynı isimde iki promptu olamaz
            if (await _repo.AnyAsync(p => p.UserId == userId && p.Name == name, ct))
                throw new InvalidOperationException("Bu isimde bir prompt zaten var.");

            var entity = new Prompt
            {
                Name = name,
                Category = dto.Category?.Trim(),
                Language = dto.Language?.Trim(),
                Description = dto.Description,
                IsActive = dto.IsActive,
                Body = dto.Body,
                SystemPrompt = dto.SystemPrompt
            };

            // BaseService.AddAsync kullanıyoruz (UserId'yi o set ediyor)
            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        // UPDATE (Özel Validasyonlu)
        public async Task UpdateAsync(int id, SavePromptDto dto, int userId, CancellationToken ct)
        {
            // Önce yetki ve varlık kontrolü (BaseService'den faydalanabiliriz ama custom logic var)
            var entity = await base.GetByIdAsync(id, userId, ct);

            if (entity == null)
                throw new KeyNotFoundException("Prompt bulunamadı.");

            var name = dto.Name.Trim();

            // İsim değiştiyse unique kontrolü yap
            if (!string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (await _repo.AnyAsync(p => p.UserId == userId && p.Name == name && p.Id != id, ct))
                    throw new InvalidOperationException("Bu isimde başka bir prompt zaten var.");
            }

            // Map
            entity.Name = name;
            entity.Category = dto.Category?.Trim();
            entity.Language = dto.Language?.Trim();
            entity.Description = dto.Description;
            entity.IsActive = dto.IsActive;
            entity.Body = dto.Body;
            entity.SystemPrompt = dto.SystemPrompt;

            // Base Update
            await base.UpdateAsync(entity, userId, ct);
        }
    }
}
