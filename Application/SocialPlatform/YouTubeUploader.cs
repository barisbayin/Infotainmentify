using Application.Abstractions;
using Application.Contracts.Script;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Application.SocialPlatform
{
    public class YouTubeUploader : ISocialUploader
    {
        private static readonly Dictionary<string, string> CategoryMap = new()
        {
            { "Film & Animation", "1" },
            { "Autos & Vehicles", "2" },
            { "Music", "10" },
            { "Pets & Animals", "15" },
            { "Sports", "17" },
            { "Shorts", "22" },
            { "People & Blogs", "22" }, // default
            { "Comedy", "23" },
            { "Entertainment", "24" },
            { "News & Politics", "25" },
            { "Howto & Style", "26" },
            { "Education", "27" },
            { "Science & Technology", "28" },
        };

        public async Task<string> UploadAsync(
            int userId,
            string videoPath,
            ScriptContentDto script,
            IReadOnlyDictionary<string, string> credentials,
            CancellationToken ct = default)
        {
            if (!File.Exists(videoPath))
                throw new FileNotFoundException("Video dosyası bulunamadı.", videoPath);

            // -------------------------------------------------------------
            // 1) Credentials çöz
            // -------------------------------------------------------------
            if (!credentials.TryGetValue("client_id", out var clientId) ||
                !credentials.TryGetValue("client_secret", out var clientSecret) ||
                !credentials.TryGetValue("refresh_token", out var refreshToken))
            {
                throw new InvalidOperationException("YouTube credential eksik (client_id / client_secret / refresh_token).");
            }

            var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
            {
                RefreshToken = refreshToken
            };

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { YouTubeService.Scope.YoutubeUpload }
                }
            );

            var credential = new UserCredential(flow, userId.ToString(), token);

            // -------------------------------------------------------------
            // 2) YouTube client oluştur
            // -------------------------------------------------------------
            var service = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "InfotainmentifyUploader"
            });

            // -------------------------------------------------------------
            // 3) ScriptContentDto -> YouTube Metadata Mapping
            // -------------------------------------------------------------
            var meta = script;

            string title =
                !string.IsNullOrWhiteSpace(meta.Title)
                    ? meta.Title
                    : "AI Generated Video";

            string description =
                meta.Platform!.Description ?? "";

            string[] tags =
                meta.Tags?.Any() == true
                    ? meta.Tags.ToArray()
                    : new[] { "shorts", "ai", "infotainment" };

            string categoryId =
                (meta.Category != null &&
                 CategoryMap.TryGetValue(meta.Category, out var cid))
                    ? cid
                    : "22";

            string visibility = "public";

            bool madeForKids = false;

            // -------------------------------------------------------------
            // 4) Video objesi
            // -------------------------------------------------------------
            var video = new Google.Apis.YouTube.v3.Data.Video
            {
                Snippet = new VideoSnippet
                {
                    Title = title,
                    Description = description,
                    Tags = tags,
                    CategoryId = categoryId
                },
                Status = new Google.Apis.YouTube.v3.Data.VideoStatus
                {
                    PrivacyStatus = visibility,
                    MadeForKids = madeForKids
                }
            };

            // -------------------------------------------------------------
            // 5) Upload request
            // -------------------------------------------------------------
            using var fileStream = new FileStream(videoPath, FileMode.Open);

            var request = service.Videos.Insert(
                video,
                "snippet,status",
                fileStream,
                "video/*"
            );

            request.ChunkSize = 256 * 1024;

            string uploadedVideoId = string.Empty;

            // Progress hook
            request.ProgressChanged += progress =>
            {
                if (progress.Status == UploadStatus.Failed)
                {
                    throw new InvalidOperationException(
                        $"YouTube upload failed: {progress.Exception?.Message}");
                }
            };

            request.ResponseReceived += response =>
            {
                uploadedVideoId = response.Id;
            };

            await request.UploadAsync(ct);

            if (string.IsNullOrWhiteSpace(uploadedVideoId))
                throw new InvalidOperationException("YouTube upload tamamlandı fakat video ID alınamadı.");

            return uploadedVideoId;
        }
    }
}
