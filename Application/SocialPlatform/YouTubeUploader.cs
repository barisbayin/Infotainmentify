using Application.Abstractions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Application.SocialPlatform
{
    public class YouTubeUploader : ISocialUploader
    {
        public async Task<string> UploadAsync(
            int userId,
            string videoPath,
            string? title,
            string? description,
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

            var flow = new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow(
                new Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
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
            // 3) Video metadata hazırlanır
            // -------------------------------------------------------------
            var video = new Google.Apis.YouTube.v3.Data.Video
            {
                Snippet = new VideoSnippet
                {
                    Title = title ?? "AI Generated Video",
                    Description = description ?? "",
                    CategoryId = "22", // People & Blogs (Shorts için en yaygın)
                    Tags = new[]
                    {
                    "shorts", "ai", "infotainment", "viral", "trending", "fun fact"
                }
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = "public" // public/unlisted/private
                }
            };

            // -------------------------------------------------------------
            // 4) Upload request hazırlığı
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

            // -------------------------------------------------------------
            // 5) Upload progres hook
            // -------------------------------------------------------------
            request.ProgressChanged += progress =>
            {
                switch (progress.Status)
                {
                    case UploadStatus.Uploading:
                        // istersen SignalR notify buraya ekleyebilirim
                        break;

                    case UploadStatus.Failed:
                        throw new InvalidOperationException(
                            $"YouTube upload failed: {progress.Exception?.Message}");
                }
            };

            // -------------------------------------------------------------
            // 6) Finalizasyon hook
            // -------------------------------------------------------------
            request.ResponseReceived += response =>
            {
                uploadedVideoId = response.Id;
            };

            // -------------------------------------------------------------
            // 7) Upload başlat
            // -------------------------------------------------------------
            await request.UploadAsync(ct);

            if (string.IsNullOrWhiteSpace(uploadedVideoId))
                throw new InvalidOperationException("YouTube upload tamamlandı fakat video ID alınamadı.");

            return uploadedVideoId;
        }
    }
}
