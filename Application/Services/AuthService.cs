using Application.Abstractions;
using Core.Contracts;
using Core.Entity.User;
using Core.Enums;
using Core.Security;
using Microsoft.AspNetCore.Identity;

namespace Application.Services
{
    public class AuthService
    {
        private readonly IRepository<AppUser> _repo;
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher<AppUser> _hasher;
        private readonly IUserDirectoryService _dirs;
        private readonly IJwtTokenFactory _jwt;

        public AuthService(
            IRepository<AppUser> repo,
            IUnitOfWork uow,
            IPasswordHasher<AppUser> hasher,
            IUserDirectoryService dirs,
            IJwtTokenFactory jwt)
        {
            _repo = repo;
            _uow = uow;
            _hasher = hasher;
            _dirs = dirs;
            _jwt = jwt;
        }

        // REGISTER
        public async Task<(AppUser user, string token)> RegisterAsync(
            string email, string username, string password, CancellationToken ct)
        {
            email = email.Trim().ToLowerInvariant();
            username = UserDirectoryService.SanitizeUsername(username);

            // exists checks (asNoTracking: true)
            var existsEmail = await _repo.AnyAsync(u => u.Email == email, ct);
            if (existsEmail) throw new InvalidOperationException("Email already exists.");

            var existsUser = await _repo.AnyAsync(u => u.Username == username, ct);
            if (existsUser) throw new InvalidOperationException("Username already exists.");

            var user = new AppUser
            {
                Email = email,
                Username = username,
                Role = UserRole.Normal,
                PasswordHash = "" // doldurulacak
            };
            user.PasswordHash = _hasher.HashPassword(user, password);

            // 1) Kaydet → Id oluşsun
            await _repo.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);           // Id doldu

            // 2) DirectoryName set + scaffold
            user.DirectoryName = $"{user.Id}_{user.Username}";
            _repo.Update(user);
            await _uow.SaveChangesAsync(ct);

            await _dirs.EnsureUserScaffoldAsync(user, ct);

            var token = _jwt.CreateToken(user);
            return (user, token);
        }

        // LOGIN
        public async Task<(AppUser user, string token)> LoginAsync(string login, string password, CancellationToken ct)
        {
            var key = login.Trim().ToLowerInvariant();

            // sadece aktif kullanıcılar login olabilir
            var user = await _repo.FirstOrDefaultAsync(
                u => (u.Email == key || u.Username == key) && u.IsActive && !u.Removed,
                asNoTracking: false,
                ct: ct)
                ?? throw new InvalidOperationException("Invalid credentials.");

            var res = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (res == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Invalid credentials.");

            // DirectoryName yoksa üret
            if (string.IsNullOrWhiteSpace(user.DirectoryName))
            {
                user.DirectoryName = $"{user.Id}_{UserDirectoryService.SanitizeUsername(user.Username)}";
                _repo.Update(user);
                await _uow.SaveChangesAsync(ct);
            }

            await _dirs.EnsureUserScaffoldAsync(user, ct);

            var token = _jwt.CreateToken(user);
            return (user, token);
        }


        // Basit yardımcılar (PromptService stilinde)
        public Task<AppUser?> GetAsync(int id, CancellationToken ct)
            => _repo.GetByIdAsync(id, asNoTracking: true, ct);

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
            => _repo.AnyAsync(u => u.Email == email.ToLower().Trim(), ct);

        public Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
            => _repo.AnyAsync(u => u.Username == username.ToLower().Trim(), ct);
    }

}
