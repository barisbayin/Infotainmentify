using Application.Abstractions;
using Core.Entity.User;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

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
            if (!Directory.Exists(d))
                Directory.CreateDirectory(d);

        await Task.CompletedTask;
    }

    // ----------------------------------------
    // PIPELINE PATHS (Eksikler tamamlandı)
    // ----------------------------------------

    public string GetVideoPipelinesRoot(AppUser user)
        => Path.Combine(GetUserRoot(user), "video_pipelines");

    public string GetVideoPipelineRoot(AppUser user, int pipelineId)
        => Path.Combine(GetVideoPipelinesRoot(user), pipelineId.ToString());

    public string GetVideoPipelinesRoot(AppUser user, int pipelineId)
        => GetVideoPipelineRoot(user, pipelineId);

    public string GetPipelineAssetsRoot(AppUser user, int pipelineId)
        => Path.Combine(GetVideoPipelineRoot(user, pipelineId), "assets");

    public string GetPipelineImages(AppUser user, int pipelineId)
        => Path.Combine(GetPipelineAssetsRoot(user, pipelineId), "images");

    public string GetPipelineAudio(AppUser user, int pipelineId)
        => Path.Combine(GetPipelineAssetsRoot(user, pipelineId), "audio");

    public string GetPipelineRaw(AppUser user, int pipelineId)
        => Path.Combine(GetPipelineAssetsRoot(user, pipelineId), "raw");

    public string GetPipelineRenderRoot(AppUser user, int pipelineId)
        => Path.Combine(GetVideoPipelineRoot(user, pipelineId), "render");

    public string GetPipelineSceneRenders(AppUser user, int pipelineId)
        => Path.Combine(GetPipelineRenderRoot(user, pipelineId), "scenes");

    public string GetPipelineMergedRender(AppUser user, int pipelineId)
        => Path.Combine(GetPipelineRenderRoot(user, pipelineId), "merged");

    public string GetPipelineFinalRoot(AppUser user, int pipelineId)
        => Path.Combine(GetVideoPipelineRoot(user, pipelineId), "final");

    public string GetPipelineTemp(AppUser user, int pipelineId)
        => Path.Combine(GetVideoPipelineRoot(user, pipelineId), "temp");

    public static string SanitizeUsername(string username) { var u = username.Trim().ToLowerInvariant().Replace(' ', '_'); u = Regex.Replace(u, @"[^a-z0-9_]", ""); return string.IsNullOrWhiteSpace(u) ? "user" : u; }



    // Implementation (UserDirectoryService.cs)
    public async Task<string> GetRunDirectoryAsync(int userId, int runId, string subFolder)
    {
        // Base path: "wwwroot/users/{userId}/runs/{runId}/{subFolder}"
        var userRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "UserFiles", "User_" + userId.ToString());
        var runPath = Path.Combine(userRoot, "runs", "Run_" + runId.ToString(), subFolder);

        if (!Directory.Exists(runPath))
        {
            Directory.CreateDirectory(runPath);
        }

        return await Task.FromResult(runPath);
    }

    public async Task<string> GetDefaultBlackBackground()
    => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets", "Image" ,"black.png");

    public async Task<string> GetPublicUrl(string physicalPath)
    {
        // Gelen Yol: C:\...\ALL_FILES\UserFiles\User_1\runs\Run_10\images\resim.png
        // Hedef URL: /files/UserFiles/User_1/runs/Run_10/images/resim.png

        var keyword = "ALL_FILES";
        var index = physicalPath.IndexOf(keyword);

        if (index == -1) return physicalPath; // Hata veya zaten url ise karışma

        // "ALL_FILES" sonrasını al: "\UserFiles\User_1\..."
        var relativePath = physicalPath.Substring(index + keyword.Length);

        // Windows ters slaşlarını (\) web slaşına (/) çevir
        var webPath = relativePath.Replace("\\", "/");

        // Başına sanal yolu (/files) ekle
        return $"/files{webPath}";
    }
}
