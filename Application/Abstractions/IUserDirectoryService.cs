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

        // ------------------------------
        // VIDEO PIPELINE PATHS (YENİ!)
        // ------------------------------

        string GetVideoPipelinesRoot(AppUser user);                        // .../video_pipelines/
        string GetVideoPipelineRoot(AppUser user, int pipelineId);         // .../video_pipelines/{id}/

        string GetPipelineAssetsRoot(AppUser user, int pipelineId);        // .../video_pipelines/{id}/assets/
        string GetPipelineImages(AppUser user, int pipelineId);            // .../assets/images/
        string GetPipelineAudio(AppUser user, int pipelineId);             // .../assets/audio/
        string GetPipelineRaw(AppUser user, int pipelineId);               // .../assets/raw/

        string GetPipelineRenderRoot(AppUser user, int pipelineId);        // .../render/
        string GetPipelineSceneRenders(AppUser user, int pipelineId);      // .../render/scenes/
        string GetPipelineMergedRender(AppUser user, int pipelineId);      // .../render/merged/

        string GetPipelineFinalRoot(AppUser user, int pipelineId);         // .../final/
        string GetPipelineTemp(AppUser user, int pipelineId);              // .../temp/

        // ------------------------------
        // Scaffold
        // ------------------------------

        Task EnsureUserScaffoldAsync(AppUser user, CancellationToken ct = default);
    }
}
