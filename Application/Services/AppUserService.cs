using Application.Abstractions;
using Application.Contracts.AppUser;
using Application.Mappers;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services
{
    public class AppUserService
    {
        private readonly IRepository<AppUser> _repo;
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher<AppUser> _hasher;
        private readonly ICurrentUserService _current;
        private readonly IUserDirectoryService _dirs;

        public AppUserService(
            IRepository<AppUser> repo,
            IUnitOfWork uow,
            IPasswordHasher<AppUser> hasher,
            ICurrentUserService current,
            IUserDirectoryService dirs)
        {
            _repo = repo; _uow = uow; _hasher = hasher; _current = current; _dirs = dirs;
        }

        // Tüm kullanıcılar (Admin: hepsi, Normal: sadece kendini görmek istersen FE'de /me kullan)
        public async Task<IReadOnlyList<AppUserListDto>> ListAsync(CancellationToken ct)
        {
            var users = await _repo.GetAllAsync(asNoTracking: true, ct);
            return users.Select(x => x.ToListDto()).ToList();
        }

        // Id ile detay
        public async Task<AppUserDetailDto?> GetAsync(int id, CancellationToken ct)
        {
            var u = await _repo.GetByIdAsync(id, asNoTracking: true, ct);
            return u?.ToDetailDto();
        }

        // Me
        public async Task<AppUserDetailDto?> MeAsync(CancellationToken ct)
        {
            if (_current.UserId <= 0) return null;
            var u = await _repo.GetByIdAsync(_current.UserId, asNoTracking: true, ct);
            return u?.ToDetailDto();
        }

        // Profil güncelle (ben)
        public async Task UpdateMeAsync(string email, string username, CancellationToken ct)
        {
            if (_current.UserId <= 0) throw new InvalidOperationException("Unauthorized");

            email = email.Trim().ToLowerInvariant();
            username = UserDirectoryService.SanitizeUsername(username);

            if (await _repo.AnyAsync(x => x.Email == email && x.Id != _current.UserId, ct))
                throw new InvalidOperationException("Email already exists.");

            if (await _repo.AnyAsync(x => x.Username == username && x.Id != _current.UserId, ct))
                throw new InvalidOperationException("Username already exists.");

            var u = await _repo.GetByIdAsync(_current.UserId, asNoTracking: false, ct)
                    ?? throw new InvalidOperationException("User not found.");

            u.Email = email;
            u.Username = username;

            if (string.IsNullOrWhiteSpace(u.DirectoryName))
                u.DirectoryName = $"{u.Id}_{username}";

            _repo.Update(u);
            await _uow.SaveChangesAsync(ct);
            await _dirs.EnsureUserScaffoldAsync(u, ct);
        }

        // Şifre değiştir (ben)
        public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
        {
            if (_current.UserId <= 0) throw new InvalidOperationException("Unauthorized");

            var u = await _repo.GetByIdAsync(_current.UserId, asNoTracking: false, ct)
                    ?? throw new InvalidOperationException("User not found.");

            var vr = _hasher.VerifyHashedPassword(u, u.PasswordHash, currentPassword);
            if (vr == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Current password incorrect.");

            u.PasswordHash = _hasher.HashPassword(u, newPassword);
            _repo.Update(u);
            await _uow.SaveChangesAsync(ct);
        }

        // Admin: rol set
        public async Task SetRoleAsync(int userId, UserRole role, CancellationToken ct)
        {
            var u = await _repo.GetByIdAsync(userId, asNoTracking: false, ct)
                    ?? throw new InvalidOperationException("User not found.");
            u.Role = role;
            _repo.Update(u);
            await _uow.SaveChangesAsync(ct);
        }

        // Admin: aktif pasif (soft delete)
        public async Task SetActiveAsync(int userId, bool isActive, CancellationToken ct)
        {
            if (userId == _current.UserId && !isActive)
                throw new InvalidOperationException("You cannot deactivate your own account.");

            var u = await _repo.GetByIdAsync(userId, asNoTracking: false, ct)
                    ?? throw new InvalidOperationException("User not found.");

            u.IsActive = isActive; // 🔥 tersine çevirme yok artık
            _repo.Update(u);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
