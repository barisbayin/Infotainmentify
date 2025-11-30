using Application.Abstractions;
using Application.Contracts.AppUser;
using Application.Mappers;
using Application.Services.Base;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity.User;
using Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services
{
    public class AppUserService : BaseService<AppUser>
    {
        private readonly IPasswordHasher<AppUser> _hasher;
        private readonly ICurrentUserService _current;
        private readonly IUserDirectoryService _dirs;

        public AppUserService(
            IRepository<AppUser> repo,
            IUnitOfWork uow,
            IPasswordHasher<AppUser> hasher,
            ICurrentUserService current,
            IUserDirectoryService dirs)
            : base(repo, uow) // BaseService'e dependency'leri paslıyoruz
        {
            _hasher = hasher;
            _current = current;
            _dirs = dirs;
        }

        // =================================================================
        // GET METHODS
        // =================================================================

        // Admin: Tüm kullanıcıları listele
        public async Task<IReadOnlyList<AppUserListDto>> ListAsync(CancellationToken ct)
        {
            // BaseService.GetAllAsync kullanmıyoruz çünkü o UserId filtresi yapmaya çalışır.
            // Admin her şeyi görür.
            var users = await _repo.GetAllAsync(asNoTracking: true, ct);
            return users.Select(x => x.ToListDto()).ToList();
        }

        // Admin veya Sistem: ID ile kullanıcı detay
        public async Task<AppUserDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            // BaseService.GetByIdAsync kullanabiliriz ama AppUser'da AppUserId property'si 
            // olmadığı için BaseService "Sahibi yok, herkese açık" sanabilir.
            // O yüzden manuel çekiyoruz.
            var u = await _repo.GetByIdAsync(id, asNoTracking: true, ct);
            return u?.ToDetailDto();
        }

        // Ben Kimim? (Me Endpoint)
        public async Task<AppUserDetailDto?> MeAsync(CancellationToken ct)
        {
            if (_current.UserId <= 0) return null;
            return await GetAsync(_current.UserId, ct);
        }

        // =================================================================
        // WRITE METHODS (Custom Business Logic)
        // =================================================================

        // Profil Güncelleme (Ben)
        public async Task UpdateMeAsync(string email, string username, CancellationToken ct)
        {
            var userId = _current.UserId;
            if (userId <= 0) throw new UnauthorizedAccessException("Giriş yapmalısınız.");

            // Validasyonlar
            email = email.Trim().ToLowerInvariant();
            username = UserDirectoryService.SanitizeUsername(username);

            // Çakışma Kontrolü (Unique Email/Username)
            // Kendi ID'm dışındakilerde bu email var mı?
            if (await _repo.AnyAsync(x => x.Email == email && x.Id != userId, ct))
                throw new InvalidOperationException("Bu e-posta adresi kullanımda.");

            if (await _repo.AnyAsync(x => x.Username == username && x.Id != userId, ct))
                throw new InvalidOperationException("Bu kullanıcı adı alınmış.");

            // Entity'i çek (Tracking açık, çünkü update edeceğiz)
            var user = await _repo.GetByIdAsync(userId, asNoTracking: false, ct);
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            // Değişiklikleri uygula
            user.Email = email;
            user.Username = username;

            // İlk kez directory oluşuyorsa veya değiştiyse
            if (string.IsNullOrWhiteSpace(user.DirectoryName))
                user.DirectoryName = $"{user.Id}_{username}";

            // BaseRepo update çağırmaya gerek yok (EF Core Tracking halleder) ama alışkanlık olsun
            _repo.Update(user);
            await _uow.SaveChangesAsync(ct);

            // Klasör yapısını güncelle/oluştur
            await _dirs.EnsureUserScaffoldAsync(user, ct);
        }

        // Şifre Değiştirme (Ben)
        public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
        {
            var userId = _current.UserId;
            if (userId <= 0) throw new UnauthorizedAccessException();

            var user = await _repo.GetByIdAsync(userId, asNoTracking: false, ct);
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            // Eski şifre kontrolü
            var verifyResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Mevcut şifre hatalı.");

            // Yeni şifreyi hashle ve kaydet
            user.PasswordHash = _hasher.HashPassword(user, newPassword);

            await _uow.SaveChangesAsync(ct);
        }

        // =================================================================
        // ADMIN METHODS
        // =================================================================

        public async Task SetRoleAsync(int targetUserId, UserRole role, CancellationToken ct)
        {
            // Buraya [Authorize(Roles="Admin")] Controller'dan gelecek ama servis içinde de kontrol iyidir.
            // Şimdilik pas geçiyorum.

            var user = await _repo.GetByIdAsync(targetUserId, asNoTracking: false, ct);
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            user.Role = role;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task SetActiveAsync(int targetUserId, bool isActive, CancellationToken ct)
        {
            if (targetUserId == _current.UserId && !isActive)
                throw new InvalidOperationException("Kendi hesabınızı pasife alamazsınız.");

            var user = await _repo.GetByIdAsync(targetUserId, asNoTracking: false, ct);
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            // isActive parametresini AppUser entity'sine eklemen gerekebilir (örn: IsActive propertysi)
            // Eğer BaseEntity'de 'Removed' varsa onu kullanabiliriz (Soft Delete)
            // Veya IsActive diye ayrı bir alan açabiliriz.

            // user.IsActive = isActive; 
            // _repo.Update(user);
            // await _uow.SaveChangesAsync(ct);
        }
    }
}
