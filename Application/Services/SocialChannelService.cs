using Application.Abstractions;
using Application.Services.Base;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    public class SocialChannelService : BaseService<SocialChannel>
    {
        private readonly ISecretStore _secretStore;

        public SocialChannelService(
            IRepository<SocialChannel> repo,
            IUnitOfWork uow,
            ISecretStore secretStore) : base(repo, uow)
        {
            _secretStore = secretStore;
        }

        // BaseService zaten GetByIdAsync, GetAllAsync (Entity döner) sağlıyor.
        // Ekstra bir ListDto metoduna gerek yok.

        // CREATE (Entity alır, Entity döner/işler)
        // DTO parçalaması Controller'da yapıldı, buraya ham veri geldi.
        public async Task CreateChannelAsync(SocialChannel entity, string? rawTokens, int userId, CancellationToken ct)
        {
            // İş Kuralı: Aynı isimde kanal var mı?
            var name = entity.ChannelName?.Trim();
            if (await _repo.AnyAsync(x => x.AppUserId == userId && x.ChannelName == name, ct))
                throw new InvalidOperationException("Bu isimde bir kanal zaten var.");

            // Şifreleme mantığı servisin sorumluluğundadır
            if (!string.IsNullOrEmpty(rawTokens))
            {
                entity.EncryptedTokensJson = _secretStore.Protect(rawTokens);
            }

            await base.AddAsync(entity, userId, ct);
        }

        // UPDATE
        public async Task UpdateChannelAsync(SocialChannel entity, string? rawTokens, int userId, CancellationToken ct)
        {
            // Mevcut veriyi çek (BaseService metodunu kullanabilirsin veya repo)
            // Ama entity zaten Controller'dan dolu geliyorsa direkt logic işletiriz.

            // Burada genelde "Partial Update" zor olduğu için, 
            // Controller'da Entity'i DB'den çekip, DTO ile güncelleyip, buraya yollamak en temizidir.

            if (!string.IsNullOrEmpty(rawTokens))
            {
                entity.EncryptedTokensJson = _secretStore.Protect(rawTokens);
            }

            await base.UpdateAsync(entity, userId, ct);
        }

        // Executor için özel metod (Entity döner)
        public async Task<SocialChannel> GetActiveChannelAsync(int id, int userId, CancellationToken ct)
        {
            var entity = await GetByIdAsync(id, userId, ct);
            if (entity == null) throw new Exception("Kanal yok");
            // ... active kontrolü ...
            return entity;
        }
    }
}

