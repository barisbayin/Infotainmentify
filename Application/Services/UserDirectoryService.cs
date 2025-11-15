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

        public string GetScriptDirectory(int userId, int scriptId)
        {
            var userDir = Path.Combine(GetAppFilesRoot(), $"user_{userId}");
            var dir = Path.Combine(userDir, "video_projects", $"script_{scriptId:D6}");
            Directory.CreateDirectory(dir);
            return dir;
        }

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


        public string GetVideoPipelinesRoot(AppUser user, int pipelineId)
            => Path.Combine(GetUserRoot(user), "video_pipelines", pipelineId.ToString());

        public string GetPipelineAssetsRoot(AppUser user, int pipelineId)
            => Path.Combine(GetVideoPipelinesRoot(user, pipelineId), "assets");

        public string GetPipelineImages(AppUser user, int pipelineId)
            => Path.Combine(GetPipelineAssetsRoot(user, pipelineId), "images");

        public string GetPipelineAudio(AppUser user, int pipelineId)
            => Path.Combine(GetPipelineAssetsRoot(user, pipelineId), "audio");

        public string GetPipelineRenderRoot(AppUser user, int pipelineId)
            => Path.Combine(GetVideoPipelinesRoot(user, pipelineId), "render");

        public string GetPipelineFinalRoot(AppUser user, int pipelineId)
            => Path.Combine(GetVideoPipelinesRoot(user, pipelineId), "final");

        public string GetPipelineTemp(AppUser user, int pipelineId)
            => Path.Combine(GetVideoPipelinesRoot(user, pipelineId), "temp");



        public string GetPipelineRaw(AppUser user, int pipelineId)
        {
            throw new NotImplementedException();
        }


        public string GetPipelineSceneRenders(AppUser user, int pipelineId)
        {
            throw new NotImplementedException();
        }

        public string GetPipelineMergedRender(AppUser user, int pipelineId)
        {
            throw new NotImplementedException();
        }

        public string GetVideoPipelinesRoot(AppUser user)
        {
            throw new NotImplementedException();
        }

        public string GetVideoPipelineRoot(AppUser user, int pipelineId)
        {
            throw new NotImplementedException();
        }
    }
}
