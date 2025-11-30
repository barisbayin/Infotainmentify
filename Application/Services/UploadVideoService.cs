using Application.Abstractions;
using Application.Contracts.Script;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Entity.User;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    public class UploadVideoService
    {
        private readonly IRepository<ContentPipelineRun_> _pipelineRepo;
        private readonly IRepository<UserSocialChannel> _channelRepo;
        private readonly ISocialUploaderFactory _uploaderFactory;
        private readonly ISecretStore _secret;
        private readonly INotifierService _notifier;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public UploadVideoService(
            IRepository<ContentPipelineRun_> pipelineRepo,
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

        public async Task UploadAsync(int pipelineId, int? userId, CancellationToken ct = default)
        {
            if (!userId.HasValue)
                userId = _current.UserId;

            // ------------------------------------------------------------
            // 1) Pipeline + Script + Profile + Connections yükle
            // ------------------------------------------------------------
            var pipeline = await _pipelineRepo.FirstOrDefaultAsync(
                x => x.Id == pipelineId,
                include: q => q.Include(p => p.Script)
                               .Include(p => p.Profile)
                                   .ThenInclude(pr => pr.ScriptGenerationProfile)
                                       .ThenInclude(sg => sg.ImageAiConnection)
                               .Include(p => p.Profile)
                                   .ThenInclude(pr => pr.ScriptGenerationProfile)
                                       .ThenInclude(sg => sg.TtsAiConnection),
                asNoTracking: false,
                ct: ct
            ) ?? throw new InvalidOperationException("Pipeline bulunamadı.");

            // ------------------------------------------------------------
            // 2) Validasyon
            // ------------------------------------------------------------
            if (pipeline.ScriptId == null)
                throw new InvalidOperationException("Pipeline için script atanmadı.");

            if (pipeline.Script == null)
                throw new InvalidOperationException("Pipeline script nesnesi yüklenemedi.");

            if (string.IsNullOrWhiteSpace(pipeline.VideoPath))
                throw new InvalidOperationException("Final video render edilmemiş. Upload yapılamaz.");

            if (pipeline.Profile?.SocialChannelId == null)
                throw new InvalidOperationException("Bu profil için SocialChannel tanımlı değil.");

            // ------------------------------------------------------------
            // 3) Script DTO'yu DB’den yükle
            // ------------------------------------------------------------
            var scriptJson = pipeline.Script.Content;

            var dto = JsonSerializer.Deserialize<ScriptContentDto>(scriptJson)
                ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

            // ------------------------------------------------------------
            // 4) Sosyal Kanal bilgisi
            // ------------------------------------------------------------
            var channel = await _channelRepo.GetByIdAsync(
                pipeline.Profile.SocialChannelId.Value,
                true,
                ct
            ) ?? throw new InvalidOperationException("UserSocialChannel bulunamadı.");

            if (!channel.IsActive)
                throw new InvalidOperationException("Bu sosyal kanal pasif durumda.");

            // ------------------------------------------------------------
            // 5) Credential çöz
            // ------------------------------------------------------------
            var credsJson = _secret.Unprotect(channel.EncryptedTokensJson);
            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Upload credential JSON hatalı.");

            // ------------------------------------------------------------
            // 6) Uploader seç
            // ------------------------------------------------------------
            var uploader = _uploaderFactory.Resolve(channel.ChannelType);

            await _notifier.JobProgressAsync(
                userId.Value,
                pipelineId,
                "📤 Video upload başlatıldı...",
                0
            );

            string uploadedVideoId = string.Empty;

            try
            {
                // ------------------------------------------------------------
                // 7) Upload işlemi
                // ------------------------------------------------------------
                uploadedVideoId = await uploader.UploadAsync(
                    userId.Value,
                    pipeline.VideoPath,
                    dto,               // 🔥 Artık ScriptContentDto'yu veriyoruz
                    credentials,
                    ct
                );

                // ------------------------------------------------------------
                // 8) Pipeline güncelle
                // ------------------------------------------------------------
                pipeline.Uploaded = true;
                pipeline.UploadedAt = DateTime.Now;
                pipeline.UploadedPlatform = channel.ChannelType.ToString();
                pipeline.UploadedVideoId = uploadedVideoId;
                pipeline.Status = ContentPipelineStatus.Completed;

                _pipelineRepo.Update(pipeline);
                await _uow.SaveChangesAsync(ct);

                await _notifier.JobCompletedAsync(
                    userId.Value,
                    pipelineId,
                    true,
                    $"📤 Upload tamamlandı! VideoId = {uploadedVideoId}"
                );
            }
            catch (Exception ex)
            {
                pipeline.Status = ContentPipelineStatus.Failed;
                pipeline.ErrorMessage = ex.Message;

                _pipelineRepo.Update(pipeline);
                await _uow.SaveChangesAsync(ct);

                await _notifier.NotifyUserAsync(
                    userId.Value,
                    "pipeline.upload_error",
                    new { pipelineId, error = ex.Message }
                );

                throw;
            }
        }

    }
}
