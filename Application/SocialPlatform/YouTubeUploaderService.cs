using Application.Models;
using Application.Services.Interfaces;
using Core.Entity;
using Core.Enums;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.SocialPlatform
{
    public class YouTubeUploaderService : ISocialPlatformService
    {
        // Bu servis sadece YouTube için çalışır
        public SocialChannelType Type => SocialChannelType.YouTube;

        public async Task<string> UploadAsync(SocialChannel channel, string videoPath, SocialMetadata metadata, CancellationToken ct = default)
        {
            // 1. KİMLİK DOĞRULAMA (AUTH)
            // Veritabanındaki JSON'dan Refresh Token'ı söküp alıyoruz
            var tokenData = ParseToken(channel.EncryptedTokensJson);
            if (string.IsNullOrEmpty(tokenData.RefreshToken))
                throw new UnauthorizedAccessException("YouTube Refresh Token bulunamadı. Lütfen kanalı tekrar bağlayın.");

            var clientSecrets = new ClientSecrets
            {
                ClientId = tokenData.ClientId,
                ClientSecret = tokenData.ClientSecret
            };

            // Google kütüphanesi Refresh Token sayesinde Access Token'ı otomatik yeniler
            var tokenResponse = new TokenResponse { RefreshToken = tokenData.RefreshToken };

            var credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer { ClientSecrets = clientSecrets }
                ),
                "user",
                tokenResponse
            );

            // 2. SERVİS BAŞLATMA
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Infotainmentify AI"
            });

            // 3. VİDEO METADATA HAZIRLIĞI
            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = metadata.Title;
            video.Snippet.Description = metadata.Description;
            video.Snippet.Tags = metadata.Tags;
            video.Snippet.CategoryId = "22"; // 22 = People & Blogs (Varsayılan güvenli kategori)
            // İstersen CategoryId'yi de Config'den alabilirsin.

            video.Status = new VideoStatus();
            // Privacy: "private", "public", "unlisted"
            video.Status.PrivacyStatus = metadata.PrivacyStatus.ToLowerInvariant();

            // Shorts Logic: Eğer video dikeyse YouTube otomatik Shorts yapıyor, 
            // ama yine de başlığa #Shorts eklemek iyi bir pratik (Executor'da yapıyoruz zaten).
            video.Status.SelfDeclaredMadeForKids = false; // Çocuklara özel değil (Genelde false olmalı)

            // 4. YÜKLEME İŞLEMİ (STREAM)
            using var fileStream = new FileStream(videoPath, FileMode.Open);

            // "snippet,status" -> Hangi alanları gönderdiğimizi belirtiyoruz
            var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");

            // Upload ilerlemesini takip etmek istersen buraya event bağlayabilirsin
            // videosInsertRequest.ProgressChanged += ...

            var uploadResult = await videosInsertRequest.UploadAsync(ct);

            if (uploadResult.Status != UploadStatus.Completed)
            {
                // Hata detayını yakala
                throw new Exception($"YouTube Upload Failed: {uploadResult.Exception?.Message ?? "Unknown Error"}");
            }

            // 5. SONUÇ
            var videoId = videosInsertRequest.ResponseBody.Id;
            return $"https://www.youtube.com/watch?v={videoId}";
        }

        // --- HELPER: JSON PARSE ---
        private AuthTokenModel ParseToken(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new AuthTokenModel();
            try
            {
                // Channel nesnesinde tokenları şifreli tutuyorsan burada önce Decrypt etmelisin!
                // Şimdilik düz JSON varsayıyoruz.
                return JsonSerializer.Deserialize<AuthTokenModel>(json) ?? new AuthTokenModel();
            }
            catch
            {
                return new AuthTokenModel();
            }
        }

        // DB'deki JSON yapına uygun model
        private class AuthTokenModel
        {
            [JsonPropertyName("access_token")] // JSON'daki tam karşılığı
            public string? AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }

            // Resimde gördüğüm diğer alanlar (Lazım olursa diye)
            [JsonPropertyName("scope")]
            public string? Scope { get; set; }

            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }

            [JsonPropertyName("client_id")]
            public string? ClientId { get; set; }

            [JsonPropertyName("client_secret")]
            public string? ClientSecret { get; set; }
        }
    }
}
