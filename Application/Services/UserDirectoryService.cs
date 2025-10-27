using Application.Abstractions;
using Core.Entity;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public sealed class UserDirectoryService : IUserDirectoryService
    {
        private readonly IHostEnvironment _env;
        public UserDirectoryService(IHostEnvironment env) => _env = env;

        private static readonly string AppFilesFolderName = "APP_FILES";

        public string GetAppFilesRoot()
            => Path.Combine(_env.ContentRootPath, AppFilesFolderName);

        public string GetUserRoot(AppUser user)
            => Path.Combine(GetAppFilesRoot(), user.DirectoryName);

        public string GetUserBaseFiles(AppUser user)
            => Path.Combine(GetUserRoot(user), "user_base_files");

        public string GetVideoProjects(AppUser user)
            => Path.Combine(GetUserRoot(user), "video_projects");

        public string GetRenders(AppUser user)
            => Path.Combine(GetUserRoot(user), "renders");

        public string GetTemp(AppUser user)
            => Path.Combine(GetUserRoot(user), "temp");

        public string GetCache(AppUser user)
            => Path.Combine(GetUserRoot(user), "cache");

        public async Task EnsureUserScaffoldAsync(AppUser user, CancellationToken ct = default)
        {
            var root = GetAppFilesRoot();
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            var dirs = new[]
            {
            GetUserRoot(user),
            GetUserBaseFiles(user),
            GetVideoProjects(user),
            GetRenders(user),
            GetTemp(user),
            GetCache(user)
        };
            foreach (var d in dirs)
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);

            await Task.CompletedTask;
        }

        // Yardımcı: username normalize (harf/rakam/altçizgi)
        public static string SanitizeUsername(string username)
        {
            var u = username.Trim().ToLowerInvariant().Replace(' ', '_');
            u = Regex.Replace(u, @"[^a-z0-9_]", "");
            return string.IsNullOrWhiteSpace(u) ? "user" : u;
        }
    }
}
