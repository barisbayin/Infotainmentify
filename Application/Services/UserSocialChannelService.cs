using Application.Abstractions;
using Application.Contracts.Enums;
using Application.Contracts.UserSocialChannel;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace Application.Services
{
    public class UserSocialChannelService
    {
        private readonly IRepository<UserSocialChannel> _repo;
        private readonly IRepository<AppUser> _userRepo; // Password verify için
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly ISecretStore _secret; // Token şifreleme için
        private readonly IPasswordHasher<AppUser> _hasher; // Password verify için

        public UserSocialChannelService(
            IRepository<UserSocialChannel> repo,
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
            // UserAiConnectionService'ten kopyalandı
            if (_current.UserId <= 0) throw new InvalidOperationException("Unauthorized");
            return _current.UserId;
        }

        // ----------------- Queries -----------------

        public async Task<IReadOnlyList<UserSocialChannelListDto>> ListAsync(CancellationToken ct)
        {
            var userId = RequireUser();

            // (AppUserId'nin int olduğunu varsayarak)
            var list = await _repo.FindAsync(x => x.AppUserId == userId, asNoTracking: true, ct);

            // Mapper'ı kullanıyoruz (ToListDto token içermiyor, güvenli)
            return list.Select(x => x.ToListDto()).ToList();
        }

        /// <summary>
        /// Detay: exposure=Masked (default) veya Plain. Plain istendiğinde currentPassword doğrulaması gerekir.
        /// </summary>
        public async Task<UserSocialChannelDetailsDto?> GetAsync(
            int id,
            CredentialExposure exposure = CredentialExposure.Masked,
            string? currentPassword = null,
            CancellationToken ct = default)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: true, ct);
            if (e is null || e.AppUserId != userId) return null;

            string? accessToken = null;
            string? refreshToken = null;
            var finalExposure = CredentialExposure.None;

            if (exposure is CredentialExposure.Masked or CredentialExposure.Plain)
            {
                // Şifreli JSON'ı çöz
                var tokens = SafeDecryptTokens(e.EncryptedTokensJson);

                if (exposure == CredentialExposure.Plain)
                {
                    // Güvenlik: Şifreyi doğrula (UserAiConnectionService'teki gibi)
                    if (!await VerifyCurrentUserPasswordAsync(currentPassword, ct))
                        throw new InvalidOperationException("Password verification failed.");

                    accessToken = tokens.GetValueOrDefault("accessToken");
                    refreshToken = tokens.GetValueOrDefault("refreshToken");
                    finalExposure = CredentialExposure.Plain;
                }
                else // Masked
                {
                    accessToken = Mask(tokens.GetValueOrDefault("accessToken"));
                    refreshToken = Mask(tokens.GetValueOrDefault("refreshToken"));
                    finalExposure = CredentialExposure.Masked;
                }
            }

            // Mapper KULLANILMAZ, DTO elle oluşturulur (çözülen token'ları basmak için)
            return new UserSocialChannelDetailsDto
            {
                Id = e.Id,
                ChannelType = e.ChannelType.ToString(),
                ChannelName = e.ChannelName,
                ChannelHandle = e.ChannelHandle,
                ChannelUrl = e.ChannelUrl,
                PlatformChannelId = e.PlatformChannelId,
                Scopes = e.Scopes,
                LastVerifiedAt = e.LastVerifiedAt,
                IsActive = e.IsActive,
                TokenExpiresAt = e.TokenExpiresAt,

                // Çözülmüş veya Maskelenmiş Tokenlar
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExposureLevel = finalExposure
            };
        }

        // ----------------- Commands (DetailDto ile) -----------------

        /// <summary>
        /// Create: DetailDto içinden alanları kullanır. dto.Id yok sayılır.
        /// AccessToken/RefreshToken PLAIN olarak gönderilmeli.
        /// </summary>
        public async Task<int> CreateAsync(UserSocialChannelDetailsDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            // Benzersizlik Kontrolü (Aynı kullanıcı aynı platformu 1 kez ekler)
            if (await _repo.AnyAsync(x => x.AppUserId == userId && x.ChannelType == dto.ChannelType, ct))
                throw new InvalidOperationException("This channel type is already connected.");

            ValidateTokens(dto.AccessToken); // En azından AccessToken olmalı

            // Token'ları şifrele (UserAiConnectionService'teki gibi)
            var encJson = SafeEncryptTokens(dto.AccessToken, dto.RefreshToken);

            var entity = new UserSocialChannel
            {
                AppUserId = userId,
                ChannelType = dto.ChannelType,
                ChannelName = dto.ChannelName,
                ChannelHandle = dto.ChannelHandle,
                ChannelUrl = dto.ChannelUrl,
                PlatformChannelId = dto.PlatformChannelId,
                Scopes = dto.Scopes,
                TokenExpiresAt = dto.TokenExpiresAt,
                IsActive = true,
                LastVerifiedAt = DateTime.UtcNow,

                EncryptedTokensJson = encJson // Şifreli JSON'ı kaydet
            };

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity.Id;
        }

        /// <summary>
        /// Update: DetailDto ile güncelleme. AccessToken null ise token'lara dokunma; doluysa komple değiştir (PLAIN beklenir).
        /// </summary>
        public async Task UpdateAsync(int id, UserSocialChannelDetailsDto dto, CancellationToken ct)
        {
            var userId = RequireUser();

            var e = await _repo.GetByIdAsync(id, asNoTracking: false, ct)
                ?? throw new InvalidOperationException("Channel not found.");

            if (e.AppUserId != userId)
                throw new InvalidOperationException("Forbidden");

            // ChannelType'ın (YouTube/TikTok) sonradan değiştirilmesine izin vermiyoruz (genellikle)
            // Ama diğer alanlar güncellenebilir:
            e.ChannelName = dto.ChannelName;
            e.ChannelHandle = dto.ChannelHandle;
            e.ChannelUrl = dto.ChannelUrl;
            e.PlatformChannelId = dto.PlatformChannelId;
            e.Scopes = dto.Scopes;
            e.TokenExpiresAt = dto.TokenExpiresAt;
            e.IsActive = dto.IsActive; // Aktiflik durumu da güncellenebilir

            // Token'lar DTO'da gönderildiyse (boş değilse) GÜNCELLE
            // (UserAiConnectionService'teki Credentials null kontrolü gibi)
            if (!string.IsNullOrWhiteSpace(dto.AccessToken))
            {
                ValidateTokens(dto.AccessToken);
                e.EncryptedTokensJson = SafeEncryptTokens(dto.AccessToken, dto.RefreshToken);
                e.LastVerifiedAt = DateTime.UtcNow; // Token güncellendiyse, doğrulama tarihini de güncelle
            }

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

            // EfRepository.Delete soft-delete yapıyor (UserAiConnectionService'teki gibi)
            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
        }

        // ----------------- Helpers -----------------

        // (UserAiConnectionService'ten kopyalandı)
        private async Task<bool> VerifyCurrentUserPasswordAsync(string? currentPassword, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(currentPassword)) return false;
            var u = await _userRepo.GetByIdAsync(_current.UserId, asNoTracking: true, ct);
            if (u is null) return false;
            var vr = _hasher.VerifyHashedPassword(u, u.PasswordHash, currentPassword);
            return vr != PasswordVerificationResult.Failed;
        }

        // (UserAiConnectionService'ten kopyalandı)
        private static string Mask(string? v, int leave = 4)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.Length <= leave) return new string('*', v.Length);
            return new string('*', Math.Max(0, v.Length - leave)) + v[^leave..];
        }

        // (UserAiConnection.SafeDecrypt'ten uyarlandı)
        private IReadOnlyDictionary<string, string> SafeDecryptTokens(string cipher)
        {
            if (string.IsNullOrWhiteSpace(cipher))
                return new Dictionary<string, string>();

            var json = _secret.Unprotect(cipher);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            return dict;
        }

        // (UserAiConnection.CreateAsync'teki şifrelemeden uyarlandı)
        private string SafeEncryptTokens(string? accessToken, string? refreshToken)
        {
            var dict = new Dictionary<string, string>
            {
                ["accessToken"] = accessToken ?? "",
                ["refreshToken"] = refreshToken ?? ""
            };
            var json = JsonSerializer.Serialize(dict);
            return _secret.Protect(json);
        }

        // (UserAiConnection.ValidateCredentials'ten uyarlandı)
        private static void ValidateTokens(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("AccessToken is required.");

            // (RefreshToken bazı akışlarda (örn. TikTok) opsiyoneldir veya gelmez,
            // o yüzden onu zorunlu tutmuyoruz.)
        }
    }
}
