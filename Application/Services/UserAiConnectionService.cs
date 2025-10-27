using Application.Abstractions;
using Application.Contracts.Ai;
using Application.Contracts.Enums;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace Application.Services
{
    public class UserAiConnectionService
    {
        private readonly IRepository<UserAiConnection> _repo;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly ISecretStore _secret;
        private readonly IPasswordHasher<AppUser> _hasher;

        public UserAiConnectionService(
            IRepository<UserAiConnection> repo,
            IRepository<AppUser> userRepo,
            IUnitOfWork uow,
            ICurrentUserService current,
            ISecretStore secret,
            IPasswordHasher<AppUser> hasher)
        {
            _repo = repo;
            _userRepo = userRepo;
            _uow = uow;
            _current = current;
            _secret = secret;
            _hasher = hasher;
        }

        private int RequireUser()
        {
            if (_current.UserId <= 0) throw new InvalidOperationException("Unauthorized");
            return _current.UserId;
        }

        // ----------------- Queries -----------------

        public async Task<IReadOnlyList<UserAiConnectionListDto>> ListAsync(CancellationToken ct)
        {
            var userId = RequireUser();
            var list = await _repo.FindAsync(x => x.UserId == userId, asNoTracking: true, ct);
            return list.Select(x => x.ToListDto()).ToList();
        }

        /// <summary>
        /// Detail: exposure=Masked (default) veya Plain. Plain istendiğinde currentPassword doğrulaması önerilir.
        /// </summary>
        public async Task<UserAiConnectionDetailDto?> GetAsync(
            int id,
            CredentialExposure exposure = CredentialExposure.Masked,
            string? currentPassword = null,
            CancellationToken ct = default)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: true, ct);
            if (e is null || e.UserId != userId) return null;

            IReadOnlyDictionary<string, string>? credsDto = null;
            var finalExposure = CredentialExposure.None;

            if (exposure is CredentialExposure.Masked or CredentialExposure.Plain)
            {
                var raw = SafeDecrypt(e.EncryptedCredentialJson);

                if (exposure == CredentialExposure.Plain)
                {
                    // Güvenlik için basit doğrulama
                    if (!await VerifyCurrentUserPasswordAsync(currentPassword, ct))
                        throw new InvalidOperationException("Password verification failed.");

                    credsDto = raw;                  // PLAIN
                    finalExposure = CredentialExposure.Plain;
                }
                else
                {
                    credsDto = MaskDict(raw);        // MASKED
                    finalExposure = CredentialExposure.Masked;
                }
            }

            return new UserAiConnectionDetailDto(
                e.Id,
                e.Name,
                e.Provider,
                e.AuthType,
                e.Capabilities,
                e.IsDefaultForText,
                e.IsDefaultForImage,
                e.IsDefaultForEmbedding,
                credsDto,
                finalExposure
            );
        }

        // ----------------- Commands (DetailDto ile) -----------------

        /// <summary>
        /// Create: DetailDto içinden alanları kullanır. dto.Id yok sayılır.
        /// Credentials PLAIN olarak gönderilmeli. (AuthType'a göre ValidateCredentials yapılır)
        /// </summary>
        public async Task<int> CreateAsync(UserAiConnectionDetailDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            var name = dto.Name?.Trim() ?? throw new InvalidOperationException("Name is required.");
            if (await _repo.AnyAsync(x => x.UserId == userId && x.Name == name, ct))
                throw new InvalidOperationException("Integration name already exists.");

            ValidateCredentials(dto.AuthType, dto.Credentials);

            var enc = _secret.Protect(JsonSerializer.Serialize(dto.Credentials ?? new Dictionary<string, string>()));
            var entity = new UserAiConnection
            {
                UserId = userId,
                Name = name,
                Provider = dto.Provider,
                AuthType = dto.AuthType,
                Capabilities = dto.Capabilities,
                IsDefaultForText = dto.IsDefaultForText,
                IsDefaultForImage = dto.IsDefaultForImage,
                IsDefaultForEmbedding = dto.IsDefaultForEmbedding,
                EncryptedCredentialJson = enc
            };

            await EnsureUniqueDefaultsAsync(userId, entity, isCreate: true, ct);

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity.Id;
        }

        /// <summary>
        /// Update: DetailDto ile güncelleme. Credentials null ise dokunma; doluysa komple değiştir (PLAIN beklenir).
        /// Provider/AuthType update’ini destekleyeceksek dto’daki değerleri alırız (aşağıda öyle yapıyoruz).
        /// </summary>
        public async Task UpdateAsync(int id, UserAiConnectionDetailDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Integration not found.");

            if (e.UserId != userId)
                throw new InvalidOperationException("Forbidden");

            var name = dto.Name?.Trim() ?? throw new InvalidOperationException("Name is required.");
            // Aynı isimden bir tane daha var mı? (kendisi hariç)
            if (await _repo.AnyAsync(x => x.UserId == userId && x.Name == name && x.Id != id, ct))
                throw new InvalidOperationException("Integration name already exists.");

            e.Name = name;
            e.Provider = dto.Provider;
            e.AuthType = dto.AuthType;
            e.Capabilities = dto.Capabilities;
            e.IsDefaultForText = dto.IsDefaultForText;
            e.IsDefaultForImage = dto.IsDefaultForImage;
            e.IsDefaultForEmbedding = dto.IsDefaultForEmbedding;

            if (dto.Credentials is not null)
            {
                ValidateCredentials(e.AuthType, dto.Credentials);
                e.EncryptedCredentialJson = _secret.Protect(JsonSerializer.Serialize(dto.Credentials));
            }

            await EnsureUniqueDefaultsAsync(userId, e, isCreate: false, ct);

            _repo.Update(e);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Integration not found.");

            if (e.UserId != userId)
                throw new InvalidOperationException("Forbidden");

            // EfRepository.Delete soft-delete yapıyor
            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Text/Image/Embedding için kullanıcı bazında default tekilleştir.
        /// </summary>
        public async Task SetDefaultAsync(int id, AiCapability capability, CancellationToken ct)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Integration not found.");

            if (e.UserId != userId)
                throw new InvalidOperationException("Forbidden");

            if (capability.HasFlag(AiCapability.Text)) e.IsDefaultForText = true;
            if (capability.HasFlag(AiCapability.Image)) e.IsDefaultForImage = true;
            if (capability.HasFlag(AiCapability.Embedding)) e.IsDefaultForEmbedding = true;

            await EnsureUniqueDefaultsAsync(userId, e, isCreate: false, ct);

            _repo.Update(e);
            await _uow.SaveChangesAsync(ct);
        }

        // ----------------- Helpers -----------------

        private async Task EnsureUniqueDefaultsAsync(int userId, UserAiConnection updated, bool isCreate, CancellationToken ct)
        {
            // Bu kullanıcıdaki diğer kayıtların default bayraklarını temizle
            var others = await _repo.FindAsync(x => x.UserId == userId, asNoTracking: false, ct);

            foreach (var o in others)
            {
                if (!isCreate && o.Id == updated.Id) continue;

                var changed = false;

                if (updated.IsDefaultForText && o.IsDefaultForText) { o.IsDefaultForText = false; changed = true; }
                if (updated.IsDefaultForImage && o.IsDefaultForImage) { o.IsDefaultForImage = false; changed = true; }
                if (updated.IsDefaultForEmbedding && o.IsDefaultForEmbedding) { o.IsDefaultForEmbedding = false; changed = true; }

                if (changed) _repo.Update(o);
            }
        }

        private static void ValidateCredentials(AiAuthType auth, IReadOnlyDictionary<string, string>? creds)
        {
            creds ??= new Dictionary<string, string>();

            switch (auth)
            {
                case AiAuthType.ApiKey:
                    if (!creds.TryGetValue("apiKey", out var ak) || string.IsNullOrWhiteSpace(ak))
                        throw new InvalidOperationException("apiKey is required.");
                    break;

                case AiAuthType.ApiKeySecret:
                    if (!creds.TryGetValue("apiKey", out var k) || string.IsNullOrWhiteSpace(k))
                        throw new InvalidOperationException("apiKey is required.");
                    if (!creds.TryGetValue("apiSecret", out var s) || string.IsNullOrWhiteSpace(s))
                        throw new InvalidOperationException("apiSecret is required.");
                    break;

                case AiAuthType.OAuth2:
                    // İlk kurulumda OAuth redirect/callback ile tamamlayacaksan boş olabilir.
                    // Doğrudan token verilecekse burada accessToken vb. kontrol edebilirsin.
                    break;
            }
        }

        private IReadOnlyDictionary<string, string> SafeDecrypt(string cipher)
        {
            var json = _secret.Unprotect(cipher);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            return dict;
        }

        private static IReadOnlyDictionary<string, string> MaskDict(IReadOnlyDictionary<string, string> src)
        {
            var dst = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in src)
                dst[kv.Key] = Mask(kv.Value);
            return dst;
        }

        private static string Mask(string? v, int leave = 4)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.Length <= leave) return new string('*', v.Length);
            return new string('*', Math.Max(0, v.Length - leave)) + v[^leave..];
        }

        private async Task<bool> VerifyCurrentUserPasswordAsync(string? currentPassword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(currentPassword)) return false;
            var u = await _userRepo.GetByIdAsync(_current.UserId, asNoTracking: true, ct);
            if (u is null) return false;
            var vr = _hasher.VerifyHashedPassword(u, u.PasswordHash, currentPassword);
            return vr != PasswordVerificationResult.Failed;
        }
    }
}
