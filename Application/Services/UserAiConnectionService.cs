using Application.Abstractions;
using Application.Contracts.UserAiConnection;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using System.Text.Json;

namespace Application.Services
{
    public class UserAiConnectionService
    {
        private readonly IRepository<UserAiConnection> _repo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly ISecretStore _secret;

        public UserAiConnectionService(
            IRepository<UserAiConnection> repo,
            IUnitOfWork uow,
            ICurrentUserService current,
            ISecretStore secret)
        {
            _repo = repo;
            _uow = uow;
            _current = current;
            _secret = secret;
        }

        private int RequireUser()
        {
            if (_current.UserId <= 0)
                throw new InvalidOperationException("Unauthorized");
            return _current.UserId;
        }

        // ----------------- QUERIES -----------------

        public async Task<IReadOnlyList<UserAiConnectionListDto>> ListAsync(CancellationToken ct)
        {
            var userId = RequireUser();
            var list = await _repo.FindAsync(x => x.UserId == userId, asNoTracking: true, ct);
            return list.Select(x => new UserAiConnectionListDto
            {
                Id = x.Id,
                Name = x.Name,
                TextModel = x.TextModel,
                ImageModel = x.ImageModel,
                VideoModel = x.VideoModel,
                Temperature = x.Temperature,
                Provider = x.Provider.ToString()
            }).ToList();
        }

        public async Task<UserAiConnectionDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            var userId = RequireUser();
            var e = await _repo.GetByIdAsync(id, asNoTracking: true, ct);
            if (e is null || e.UserId != userId) return null;

            var creds = SafeDecrypt(e.EncryptedCredentialJson);

            return new UserAiConnectionDetailDto
            {
                Id = e.Id,
                Name = e.Name,
                Provider = e.Provider.ToString(),
                TextModel = e.TextModel,
                ImageModel = e.ImageModel,
                VideoModel = e.VideoModel,
                Temperature = e.Temperature,
                Credentials = creds
            };
        }

        // ----------------- COMMANDS -----------------

        public async Task<int> CreateAsync(UserAiConnectionDetailDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Name is required.");

            var exists = await _repo.AnyAsync(x => x.UserId == userId && x.Name == dto.Name, ct);
            if (exists)
                throw new InvalidOperationException("Connection name already exists.");

            var enc = _secret.Protect(JsonSerializer.Serialize(dto.Credentials ?? new Dictionary<string, string>()));

            var entity = new UserAiConnection
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Provider = Enum.Parse<AiProviderType>(dto.Provider),
                TextModel = dto.TextModel,
                ImageModel = dto.ImageModel,
                VideoModel = dto.VideoModel,
                Temperature = dto.Temperature,
                EncryptedCredentialJson = enc
            };

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task UpdateAsync(int id, UserAiConnectionDetailDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Connection not found.");

            if (e.UserId != userId)
                throw new InvalidOperationException("Forbidden");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Name is required.");

            e.Name = dto.Name.Trim();
            e.Provider = Enum.Parse<AiProviderType>(dto.Provider);
            e.TextModel = dto.TextModel;
            e.ImageModel = dto.ImageModel;
            e.VideoModel = dto.VideoModel;
            e.Temperature = dto.Temperature;
            e.EncryptedCredentialJson = _secret.Protect(JsonSerializer.Serialize(dto.Credentials ?? new Dictionary<string, string>()));

            _repo.Update(e);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var userId = RequireUser();
            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Connection not found.");

            if (e.UserId != userId)
                throw new InvalidOperationException("Forbidden");

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
        }

        // ----------------- HELPERS -----------------

        private Dictionary<string, string> SafeDecrypt(string cipher)
        {
            try
            {
                var json = _secret.Unprotect(cipher) ?? "{}";
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public async Task<UserAiConnection> GetActiveAsync(int id, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(id, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");

            if (e.UserId != _current.UserId)
                throw new InvalidOperationException("Forbidden");

            if (!e.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif durumda, işlem yapılamaz.");

            return e;
        }

    }
}
