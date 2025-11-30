using Application.Abstractions;
using Application.AiLayer.Abstract;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;

namespace Application.Services
{
    public class AutoVideoGenerationService
    {
        private readonly AutoVideoPipelineService _pipelineService;
        private readonly TopicGenerationService _topicGenerationService;
        private readonly ScriptGenerationService _scriptGenerationService;
        private readonly AssetGenerationService _assetGenerationService;
        private readonly RenderVideoService _renderService;
        private readonly UploadVideoService _uploadVideoService;

        private readonly IRepository<ContentPipelineRun_> _pipelineRepo;
        private readonly IRepository<VideoGenerationProfile> _profileRepo;
        private readonly IRepository<Topic> _topicRepo;
        private readonly IRepository<Script> _scriptRepo;
        private readonly IRepository<ScriptGenerationProfile> _scriptGenerationProfileRepo;
        private readonly IRepository<TopicGenerationProfile> _topicGenerationProfileRepo;

        private readonly IAiGeneratorFactory _factory;
        private readonly IUserDirectoryService _dir;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;
        private readonly INotifierService _notifier;

        public AutoVideoGenerationService(
            AutoVideoPipelineService pipelineService,
            TopicGenerationService topicGenerationService,
            ScriptGenerationService scriptGenerationService,
            AssetGenerationService assetGenerationService,
            RenderVideoService renderService,
            UploadVideoService uploadVideoService,

            IRepository<ContentPipelineRun_> pipelineRepo,
            IRepository<VideoGenerationProfile> profileRepo,
            IRepository<Topic> topicRepo,
            IRepository<Script> scriptRepo,
            IRepository<ScriptGenerationProfile> scriptGenerationProfileRepo,
            IRepository<TopicGenerationProfile> topicGenerationProfileRepo,

            IAiGeneratorFactory factory,
            IUserDirectoryService dir,
            IUnitOfWork uow,
            ICurrentUserService current,
            INotifierService notifier)
        {
            _pipelineService = pipelineService;
            _topicGenerationService = topicGenerationService;
            _scriptGenerationService = scriptGenerationService;
            _assetGenerationService = assetGenerationService;
            _renderService = renderService;
            _uploadVideoService = uploadVideoService;

            _pipelineRepo = pipelineRepo;
            _profileRepo = profileRepo;
            _topicRepo = topicRepo;
            _scriptRepo = scriptRepo;
            _scriptGenerationProfileRepo = scriptGenerationProfileRepo;
            _topicGenerationProfileRepo = topicGenerationProfileRepo;

            _factory = factory;
            _dir = dir;
            _uow = uow;
            _current = current;
            _notifier = notifier;
        }

        // ------------------------------
        // MAIN PIPELINE ENTRY POINT
        // ------------------------------
        public async Task<ContentPipelineRun_> RunAsync(int userId, int profileId, CancellationToken ct)
        {

            // 1) Pipeline oluştur
            var pipeline = await _pipelineService.CreatePipelineAsync(userId, profileId, ct);

            try
            {
                // ============================================================
                // 1 — TOPIC
                // ============================================================
                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Topic seçimi başlatıldı.", ct);

                await SelectTopicAsync(pipeline, ct);

                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Topic seçildi.", ct);


                // ============================================================
                // 2 — SCRIPT
                // ============================================================
                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Script üretimi başlatıldı.", ct);

                await GenerateScriptAsync(pipeline, userId, ct);

                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Script üretildi.", ct);


                // ============================================================
                // 3 — ASSETS (images + tts)
                // ============================================================
                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Asset üretimi başlatıldı.", ct);

                await _assetGenerationService.GenerateAssetsAsync(pipeline.Id, ct);

                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Asset üretimi tamamlandı.", ct);


                // ============================================================
                // 4 — RENDER
                // ============================================================
                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Render işlemi başlatıldı.", ct);

                await _renderService.RenderVideoAsync(pipeline.Id, ct);

                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                await _pipelineService.AddLogAsync(pipeline, "Render tamamlandı.", ct);


                // ============================================================
                // 5 — UPLOAD (opsiyonel)
                // ============================================================
                if (pipeline.Profile.UploadAfterRender)
                {
                    await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                    await _pipelineService.AddLogAsync(pipeline, "Upload başlatıldı.", ct);

                    await UploadVideoAsync(pipeline, userId, ct);

                    await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Pending, ct);
                    await _pipelineService.AddLogAsync(pipeline, "Video upload edildi.", ct);
                }
                else
                {
                    await _pipelineService.AddLogAsync(pipeline, "UploadAfterRender=false — upload adımı atlandı.", ct);
                }


                // ============================================================
                // 6 — FINALIZE
                // ============================================================
                await FinalizePipelineAsync(pipeline, ct);

                await _pipelineService.UpdateStatusAsync(pipeline.Id, userId, ContentPipelineStatus.Completed, ct);
                await _pipelineService.AddLogAsync(pipeline, "Pipeline başarıyla tamamlandı.", ct);
            }
            catch (Exception ex)
            {
                pipeline.Status = ContentPipelineStatus.Failed;
                pipeline.ErrorMessage = ex.Message;

                await _pipelineService.AddLogAsync(pipeline, $"ERROR: {ex.Message}", ct);
                await _uow.SaveChangesAsync(ct);

                await _notifier.NotifyUserAsync(_current.UserId, "pipeline.error", new
                {
                    pipelineId = pipeline.Id,
                    message = ex.Message
                });

                throw;
            }

            return pipeline;
        }


        private async Task SelectTopicAsync(ContentPipelineRun_ pipeline, CancellationToken ct)
        {
            if (pipeline.TopicId.HasValue)
            {
                await _pipelineService.AddLogAsync(pipeline, "Mevcut topic kullanılacak.", ct);
                return;
            }

            var videoProfile = await _profileRepo.GetByIdAsync(pipeline.ProfileId, true, ct);
            var scriptProfile = await _scriptGenerationProfileRepo.GetByIdAsync(videoProfile.ScriptGenerationProfileId, true, ct);

            // TopicGenerationProfile ID buradan geliyor
            int topicProfileId = scriptProfile.TopicGenerationProfileId;

            await _pipelineService.AddLogAsync(pipeline, "TopicGenerationService ile topic üretimi başlatılıyor.", ct);

            // 🔥 TEK TOPIC ÜRET
            var topic = await _topicGenerationService.GenerateSingleAsync(topicProfileId, ct);

            pipeline.TopicId = topic.Id;
            pipeline.UpdatedAt = DateTime.Now;

            await _uow.SaveChangesAsync(ct);
            await _pipelineService.AddLogAsync(pipeline, $"Topic seçildi: {topic.Premise}", ct);

            await _notifier.NotifyUserAsync(_current.UserId, "pipeline.topic", new
            {
                pipelineId = pipeline.Id,
                topicId = topic.Id,
                premise = topic.Premise
            });
        }
        private async Task GenerateScriptAsync(ContentPipelineRun_ pipeline, int? userId, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var topic = await _topicRepo.GetByIdAsync(pipeline.TopicId!.Value, true, ct)
                    ?? throw new Exception("Topic bulunamadı.");

                var videoProfile = await _profileRepo.GetByIdAsync(pipeline.ProfileId, true, ct);
                var scriptProfileId = videoProfile.ScriptGenerationProfileId;

                await _pipelineService.AddLogAsync(pipeline, "Script üretimi başlatıldı.", ct);

                var script = await _scriptGenerationService.GenerateSingleAsync(scriptProfileId, topic, userId, ct);

                pipeline.ScriptId = script.Id;
                pipeline.UpdatedAt = DateTime.Now;

                await _uow.SaveChangesAsync(ct);

                await _pipelineService.AddLogAsync(pipeline, "Script üretildi.", ct);

                await _notifier.NotifyUserAsync(_current.UserId, "pipeline.script", new
                {
                    pipelineId = pipeline.Id,
                    scriptId = script.Id
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Script üretiminde hata oluştu: " + ex.Message);
                throw;
            }

        }

        private async Task UploadVideoAsync(ContentPipelineRun_ pipeline, int? userId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // -----------------------------------------------------------
            // 1) Validasyon
            // -----------------------------------------------------------
            if (pipeline.Profile == null)
                throw new InvalidOperationException("Pipeline profili bulunamadı.");

            if (string.IsNullOrWhiteSpace(pipeline.VideoPath))
                throw new InvalidOperationException("Final video render edilmemiş. Upload yapılamaz.");

            if (pipeline.Profile.SocialChannelId == null)
                throw new InvalidOperationException("Bu profil için SocialChannel tanımlı değil.");

            // -----------------------------------------------------------
            // 2) Pipeline'a log ekle
            // -----------------------------------------------------------
            await _pipelineService.AddLogAsync(pipeline, "Upload başlatılıyor...", ct);

            try
            {
                await _uploadVideoService.UploadAsync(
                    pipeline.Id,
                    userId,
                    ct
                );

                await _pipelineService.AddLogAsync(
                    pipeline,
                    $"Upload tamamlandı. Platform={pipeline.UploadedPlatform}, VideoId={pipeline.UploadedVideoId}",
                    ct
                );
            }
            catch (Exception ex)
            {
                // UploadVideoService zaten pipeline üzerinde status + error set ediyor.
                // Burada sadece log ekleyip üst katmana fırlatıyoruz.

                await _pipelineService.AddLogAsync(pipeline, $"Upload hatası: {ex.Message}", ct);

                throw; // job framework yakalayacak
            }
        }

        private async Task FinalizePipelineAsync(ContentPipelineRun_ pipeline, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            await _pipelineService.AddLogAsync(pipeline, "Finalize işlemi başlatıldı.", ct);

            // -----------------------------------------------------------
            // 1) Final video var mı? (Zorunlu)
            // -----------------------------------------------------------
            if (string.IsNullOrWhiteSpace(pipeline.VideoPath))
                throw new InvalidOperationException("Finalize yapılamıyor: Render edilmiş video yok.");

            // -----------------------------------------------------------
            // 2) UploadAfterRender == false ise Uploaded bilgisi set edilmez
            // -----------------------------------------------------------
            if (!pipeline.Profile.UploadAfterRender)
            {
                await _pipelineService.AddLogAsync(pipeline,
                    "UploadAfterRender=false olduğu için upload atlandı.", ct);
            }

            // -----------------------------------------------------------
            // 3) Pipeline kapanış bilgileri
            // -----------------------------------------------------------
            pipeline.CompletedAt = DateTime.Now;
            pipeline.Status = ContentPipelineStatus.Completed;

            await _pipelineService.AddLogAsync(pipeline, "Pipeline finalize edildi.", ct);

            // -----------------------------------------------------------
            // 4) Veritabanına kaydet
            // -----------------------------------------------------------
            await _uow.SaveChangesAsync(ct);

            // -----------------------------------------------------------
            // 5) Frontend'e SignalR bildirim gönder
            // -----------------------------------------------------------
            await _notifier.NotifyUserAsync(
                pipeline.AppUserId,
                "pipeline.completed",
                new
                {
                    pipelineId = pipeline.Id,
                    video = new
                    {
                        path = pipeline.VideoPath,
                        uploaded = pipeline.Uploaded,
                        uploadPlatform = pipeline.UploadedPlatform,
                        uploadVideoId = pipeline.UploadedVideoId
                    }
                }
            );
        }

    }
}
