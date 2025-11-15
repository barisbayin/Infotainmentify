using Application.Abstractions;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    public class UploadVideoService
    {
        private readonly IRepository<AutoVideoPipeline> _pipelineRepo;
        private readonly IRepository<UserSocialChannel> _channelRepo;
        private readonly ISocialUploaderFactory _uploaderFactory;
        private readonly ISecretStore _secret;
        private readonly INotifierService _notifier;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public UploadVideoService(
            IRepository<AutoVideoPipeline> pipelineRepo,
            IRepository<UserSocialChannel> channelRepo,
            ISocialUploaderFactory uploaderFactory,
            ISecretStore secret,
            INotifierService notifier,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _pipelineRepo = pipelineRepo;
            _channelRepo = channelRepo;
            _uploaderFactory = uploaderFactory;
            _secret = secret;
            _notifier = notifier;
            _uow = uow;
            _current = current;
        }

        public async Task UploadAsync(int pipelineId, CancellationToken ct = default)
        {
            // ------------------------------------------------------------
            // 1) Pipeline yükle
            // ------------------------------------------------------------
            var pipeline = await _pipelineRepo.FirstOrDefaultAsync(
                x => x.Id == pipelineId && x.AppUserId == _current.UserId,
                include: q => q.Include(p => p.Profile),
                asNoTracking: false,
                ct: ct
            ) ?? throw new InvalidOperationException("Pipeline bulunamadı.");

            var userId = pipeline.AppUserId;

            if (string.IsNullOrWhiteSpace(pipeline.VideoPath))
                throw new InvalidOperationException("Final video bulunamadı. Render yapılmamış.");

            // ------------------------------------------------------------
            // 2) VideoGenerationProfile al
            // ------------------------------------------------------------
            var profile = pipeline.Profile
                ?? throw new InvalidOperationException("Pipeline profili bulunamadı.");

            if (profile.SocialChannelId == null)
                throw new InvalidOperationException("Bu profil bir sosyal upload kanalı tanımlamıyor.");

            // ------------------------------------------------------------
            // 3) Sosyal kanal bilgisi
            // ------------------------------------------------------------
            var channel = await _channelRepo.GetByIdAsync(profile.SocialChannelId.Value, true, ct)
                ?? throw new InvalidOperationException("UserSocialChannel bulunamadı.");

            if (!channel.IsActive)
                throw new InvalidOperationException("Bu sosyal kanal pasif durumda.");

            // ------------------------------------------------------------
            // 4) Credential çöz
            // ------------------------------------------------------------
            var credsJson = _secret.Unprotect(channel.EncryptedTokensJson);
            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Upload credential JSON hatalı.");

            // ------------------------------------------------------------
            // 5) Uploader seç
            // ------------------------------------------------------------
            var uploader = _uploaderFactory.Resolve(channel.ChannelType);

            await _notifier.JobProgressAsync(
                userId,
                pipelineId,
                "📤 Video upload başlatıldı...",
                0);

            string uploadedVideoId = string.Empty;

            try
            {
                // ------------------------------------------------------------
                // 6) Upload işlemi
                // ------------------------------------------------------------
                uploadedVideoId = await uploader.UploadAsync(
                    userId,
                    pipeline.VideoPath, // Final MP4 path
                    profile.TitleTemplate,
                    profile.DescriptionTemplate,
                    credentials,
                    ct
                );

                // ------------------------------------------------------------
                // 7) Pipeline güncelle
                // ------------------------------------------------------------
                pipeline.Uploaded = true;
                pipeline.UploadedAt = DateTime.Now;
                pipeline.UploadedPlatform = channel.ChannelType.ToString();
                pipeline.UploadedVideoId = uploadedVideoId;
                pipeline.Status = AutoVideoPipelineStatus.Completed;

                _pipelineRepo.Update(pipeline);
                await _uow.SaveChangesAsync(ct);

                await _notifier.JobCompletedAsync(
                    userId,
                    pipelineId,
                    true,
                    $"📤 Upload tamamlandı! VideoId = {uploadedVideoId}");
            }
            catch (Exception ex)
            {
                pipeline.Status = AutoVideoPipelineStatus.Failed;
                pipeline.ErrorMessage = ex.Message;

                _pipelineRepo.Update(pipeline);
                await _uow.SaveChangesAsync(ct);

                await _notifier.NotifyUserAsync(
                    userId,
                    "pipeline.upload_error",
                    new { pipelineId, error = ex.Message });

                throw;
            }
        }
    }
}
