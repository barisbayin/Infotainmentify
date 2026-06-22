using Core.Contracts;
using Core.Enums;
using Core.Security;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Core.Entity.User;

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
            var email = NormalizeEmail(_cfg["Admin:Email"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@local");
            var username = SlugifyUsername(_cfg["Admin:Username"] ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin");
            if (string.IsNullOrWhiteSpace(username)) username = "admin";

            var password = _cfg["Admin:Password"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "ChangeMe!123";
            var resetPassword = ShouldResetAdminPasswordOnStartup();

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var user = await _repo.FirstOrDefaultAsync(
                    u => u.Email == email || u.Username == username,
                    asNoTracking: false,
                    ct: ct);

                if (user is null)
                {
                    if (await _repo.AnyAsync(u => u.Role == UserRole.Admin, ct)) return;

                    user = new AppUser
                    {
                        Email = email,
                        Username = username,
                        Role = UserRole.Admin,
                        DirectoryName = "pending",
                        IsActive = true,
                        Removed = false
                    };
                    user.PasswordHash = _identityHasher.HashPassword(user, password);

                    await _repo.AddAsync(user, ct);
                    await _uow.SaveChangesAsync(ct);

                    user.DirectoryName = $"{user.Id}_{username}";
                    await _uow.SaveChangesAsync(ct);
                    return;
                }

                user.Email = email;
                user.Username = username;
                user.Role = UserRole.Admin;
                user.IsActive = true;
                user.Removed = false;
                user.RemovedAt = null;

                if (string.IsNullOrWhiteSpace(user.DirectoryName) || user.DirectoryName == "pending")
                {
                    user.DirectoryName = $"{user.Id}_{username}";
                }

                if (resetPassword || string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    user.PasswordHash = _identityHasher.HashPassword(user, password);
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);
        }

        private bool ShouldResetAdminPasswordOnStartup()
        {
            var configured = _cfg["Admin:ResetPasswordOnStartup"]
                ?? Environment.GetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_STARTUP");

            return bool.TryParse(configured, out var enabled) && enabled;
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
