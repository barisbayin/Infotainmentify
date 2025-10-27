using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Core.Security;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence
{
    public sealed class DataSeeder
    {
        private readonly IRepository<AppUser> _repo;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _cfg;
        private readonly IPasswordHasher<AppUser> _identityHasher;

        public DataSeeder(IRepository<AppUser> repo,
                          IUnitOfWork uow,
                          IConfiguration cfg,
                          IPasswordHasher<AppUser> identityHasher) // <-- inject
        {
            _repo = repo; _uow = uow; _cfg = cfg; _identityHasher = identityHasher;
        }

        public async Task SeedAdminAsync(CancellationToken ct = default)
        {
            var email = (_cfg["Admin:Email"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@local").Trim().ToLowerInvariant();
            var username = (_cfg["Admin:Username"] ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin").Trim().ToLowerInvariant();
            var password = _cfg["Admin:Password"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "ChangeMe!123";

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                if (await _repo.AnyAsync(u => u.Role == UserRole.Admin, ct)) return;
                if (await _repo.AnyAsync(u => u.Email == email || u.Username == username, ct)) return;

                var user = new AppUser
                {
                    Email = email,
                    Username = username,
                    Role = UserRole.Admin,
                    DirectoryName = "pending"
                };

                // ÖNEMLİ: Identity formatında hashle
                user.PasswordHash = _identityHasher.HashPassword(user, password);

                await _repo.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                user.DirectoryName = $"{user.Id}_{username}";
                await _uow.SaveChangesAsync(ct);
            }, ct);
        }


        private static string NormalizeEmail(string email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        private static readonly Regex _allowed = new(@"[^a-z0-9_-]", RegexOptions.Compiled);
        private static string SlugifyUsername(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var s = input.Trim().ToLowerInvariant()
                .Replace(' ', '-')
                .Replace('.', '-');
            s = _allowed.Replace(s, "-");
            // Birden fazla '-' -> tek '-'
            s = Regex.Replace(s, "-{2,}", "-");
            // baş/son '-'
            return s.Trim('-');
        }
    }
}
