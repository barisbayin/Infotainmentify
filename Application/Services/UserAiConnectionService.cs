using Application.Abstractions;
using Application.Contracts.AppUser;
using Application.Mappers;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity.User;
using Core.Enums;

namespace Application.Services
{
    public class UserAiConnectionService : BaseService<UserAiConnection>
    {
        private readonly ISecretStore _secretStore;

        public UserAiConnectionService(
            IRepository<UserAiConnection> repo,
            IUnitOfWork uow,
            ISecretStore secretStore) : base(repo, uow)
        {
            _secretStore = secretStore;
        }

        // Listeleme
        public async Task<IReadOnlyList<UserAiConnectionListDto>> ListAsync(int userId, CancellationToken ct)
        {
            var list = await GetAllAsync(userId, ct);
            return list.Select(x => x.ToListDto()).ToList();
        }

        // Detay (Maskelenmiş Key ile)
        public async Task<UserAiConnectionDetailDto?> GetDetailAsync(int id, int userId, CancellationToken ct)
        {
            var entity = await GetByIdAsync(id, userId, ct);
            if (entity == null) return null;

            return entity.ToDetailDto();
        }

        // CREATE
        public async Task<int> CreateAsync(SaveUserAiConnectionDto dto, int userId, CancellationToken ct)
        {
            // İsim çakışması kontrolü
            if (await _repo.AnyAsync(x => x.AppUserId == userId && x.Name == dto.Name, ct))
                throw new InvalidOperationException("Bu isimde bir bağlantınız zaten var.");

            // Provider Enum Parsing
            if (!Enum.TryParse<AiProviderType>(dto.Provider, true, out var provider))
                throw new ArgumentException("Geçersiz sağlayıcı (Provider).");

            var entity = new UserAiConnection
            {
                Name = dto.Name,
                Provider = provider,
                ExtraId = dto.ExtraId,
                // KRİTİK: API Key'i şifreleyip saklıyoruz
                EncryptedApiKey = _secretStore.Protect(dto.ApiKey)
            };

            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        // UPDATE (Key değişebilir veya aynı kalabilir)
        public async Task UpdateAsync(int id, SaveUserAiConnectionDto dto, int userId, CancellationToken ct)
        {
            var entity = await GetByIdAsync(id, userId, ct);
            if (entity == null) throw new KeyNotFoundException("Bağlantı bulunamadı.");

            entity.Name = dto.Name;
            entity.ExtraId = dto.ExtraId;

            // Eğer provider değişiyorsa güncelle (ama genelde değişmez)
            if (Enum.TryParse<AiProviderType>(dto.Provider, true, out var provider))
                entity.Provider = provider;

            // Eğer yeni bir API Key gönderildiyse güncelle (Boş veya *** değilse)
            if (!string.IsNullOrWhiteSpace(dto.ApiKey) && !dto.ApiKey.Contains("***"))
            {
                entity.EncryptedApiKey = _secretStore.Protect(dto.ApiKey);
            }

            await base.UpdateAsync(entity, userId, ct);
        }
    }
}
