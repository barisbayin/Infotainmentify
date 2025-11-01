using Application.Abstractions;
using Application.Contracts.UserSocialChannel;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using System.Text.Json;

namespace Application.Services
{
    public class UserSocialChannelService
    {
        private readonly IRepository<UserSocialChannel> _repo;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly ISecretStore _secret;

        public UserSocialChannelService(
            IRepository<UserSocialChannel> repo,
            IRepository<AppUser> userRepo,
            IUnitOfWork uow,
            ICurrentUserService current,
            ISecretStore secret)
        {
            _repo = repo;
            _userRepo = userRepo;
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

        // ------------------ QUERIES ------------------

        public async Task<IReadOnlyList<UserSocialChannelListDto>> ListAsync(CancellationToken ct)
        {
            var userId = RequireUser();
            var list = await _repo.FindAsync(x => x.AppUserId == userId, asNoTracking: true, ct);
            return list.Select(x => x.ToListDto()).ToList();
        }

        public async Task<UserSocialChannelDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            var userId = RequireUser();
            var e = await _repo.GetByIdAsync(id, asNoTracking: true, ct);
            if (e is null || e.AppUserId != userId) return null;

            var tokens = SafeDecrypt(e.EncryptedTokensJson);

            return new UserSocialChannelDetailDto
            {
                Id = e.Id,
                ChannelType = e.ChannelType,
                ChannelName = e.ChannelName,
                ChannelHandle = e.ChannelHandle,
                ChannelUrl = e.ChannelUrl,
                PlatformChannelId = e.PlatformChannelId,
                Tokens = tokens,
                TokenExpiresAt = e.TokenExpiresAt,
                Scopes = e.Scopes
            };
        }

        // ------------------ COMMANDS ------------------

        public async Task<int> CreateAsync(UserSocialChannelDetailDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            var name = dto.ChannelName?.Trim() ?? throw new InvalidOperationException("Channel name required.");
            if (await _repo.AnyAsync(x => x.AppUserId == userId && x.ChannelName == name, ct))
                throw new InvalidOperationException("Channel name already exists.");

            var enc = SafeEncrypt(dto.Tokens);

            var e = new UserSocialChannel
            {
                AppUserId = userId,
                ChannelType = dto.ChannelType,
                ChannelName = name,
                ChannelHandle = dto.ChannelHandle,
                ChannelUrl = dto.ChannelUrl,
                PlatformChannelId = dto.PlatformChannelId,
                EncryptedTokensJson = enc,
                TokenExpiresAt = dto.TokenExpiresAt,
                Scopes = dto.Scopes ?? "",
            };

            await _repo.AddAsync(e, ct);
            await _uow.SaveChangesAsync(ct);
            return e.Id;
        }

        public async Task UpdateAsync(int id, UserSocialChannelDetailDto dto, CancellationToken ct)
        {
            var userId = RequireUser();
            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Channel not found.");

            if (e.AppUserId != userId)
                throw new InvalidOperationException("Forbidden");

            e.ChannelType = dto.ChannelType;
            e.ChannelName = dto.ChannelName?.Trim() ?? throw new InvalidOperationException("Channel name required.");
            e.ChannelHandle = dto.ChannelHandle;
            e.ChannelUrl = dto.ChannelUrl;
            e.PlatformChannelId = dto.PlatformChannelId;
            e.Scopes = dto.Scopes ?? "";
            e.TokenExpiresAt = dto.TokenExpiresAt;

            if (dto.Tokens is { Count: > 0 })
                e.EncryptedTokensJson = SafeEncrypt(dto.Tokens);

            _repo.Update(e);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var userId = RequireUser();
            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Channel not found.");

            if (e.AppUserId != userId)
                throw new InvalidOperationException("Forbidden");

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
        }

        // ------------------ HELPERS ------------------

        private string SafeEncrypt(Dictionary<string, object>? dict)
        {
            dict ??= new Dictionary<string, object>();
            var json = JsonSerializer.Serialize(dict);
            return _secret.Protect(json);
        }

        private Dictionary<string, object> SafeDecrypt(string? cipher)
        {
            if (string.IsNullOrWhiteSpace(cipher)) return new Dictionary<string, object>();
            try
            {
                var json = _secret.Unprotect(cipher);
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                           ?? new Dictionary<string, object>();
                return dict;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public async Task<UserSocialChannel> GetActiveAsync(int id, CancellationToken ct)
        {
            var userId = RequireUser();
            var e = await _repo.GetByIdAsync(id, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("Sosyal kanal bulunamadı.");

            if (e.AppUserId != userId)
                throw new InvalidOperationException("Forbidden");

            if (!e.IsActive)
                throw new InvalidOperationException("Bu sosyal kanal pasif durumda, işlem yapılamaz.");

            return e;
        }

    }
}
