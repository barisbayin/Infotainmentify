using Core.Entity;

namespace Application.Abstractions
{
    public interface IUserDirectoryService
    {
        string GetAppFilesRoot();                           // .../APP_FILES
        string GetUserRoot(AppUser user);                   // .../APP_FILES/{Id_Username}
        string GetUserBaseFiles(AppUser user);              // .../user_base_files
        string GetVideoProjects(AppUser user);              // .../video_projects
        string GetRenders(AppUser user);                    // .../renders
        string GetTemp(AppUser user);                       // .../temp
        string GetCache(AppUser user);                      // .../cache
        string GetScriptDirectory(int userId, int scriptId);

        Task EnsureUserScaffoldAsync(AppUser user, CancellationToken ct = default);
    }
}
