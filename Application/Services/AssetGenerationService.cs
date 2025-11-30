using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Contracts.Script;
using Application.Models;
using Core.Contracts;
using Core.Entity;
using Core.Entity.User;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// Pipeline içerisindeki Script’e göre image + audio asset üretimi yapan servis.
    /// Tüm dosyalar AutoVideoPipeline dizinine yazılır, DB AssetFile olarak kaydedilir.
    /// </summary>
    public class AssetGenerationService
    {
        private readonly IRepository<ContentPipelineRun_> _pipelineRepo;
        private readonly IRepository<AutoVideoAssetFile> _assetRepo;
        private readonly IRepository<Script> _scriptRepo;
        private readonly IRepository<AppUser> _appUserRepo;
        private readonly IUnitOfWork _uow;
        private readonly IUserDirectoryService _dir;
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly INotifierService _notifier;

        public AssetGenerationService(
            IRepository<ContentPipelineRun_> pipelineRepo,
            IRepository<AutoVideoAssetFile> assetRepo,
            IRepository<Script> scriptRepo,
            IUnitOfWork uow,
            IUserDirectoryService dir,
            IAiGeneratorFactory aiFactory,
            INotifierService notifier, IRepository<AppUser> appUserRepo)
        {
            _pipelineRepo = pipelineRepo;
            _assetRepo = assetRepo;
            _scriptRepo = scriptRepo;
            _uow = uow;
            _dir = dir;
            _aiFactory = aiFactory;
            _notifier = notifier;
            _appUserRepo = appUserRepo;
        }

        // --------------------------------------------------------------------
        // MAIN
        // --------------------------------------------------------------------
        public async Task GenerateAssetsAsync(int pipelineId, CancellationToken ct)
        {
            try
            {
                var pipeline = await _pipelineRepo.FirstOrDefaultAsync(
                    x => x.Id == pipelineId,
                    include: q => q.Include(p => p.Script)
                                   .Include(p => p.Profile)
                                        .ThenInclude(p => p.ScriptGenerationProfile)
                                            .ThenInclude(s => s.ImageAiConnection)
                                   .Include(p => p.Profile)
                                        .ThenInclude(p => p.ScriptGenerationProfile)
                                            .ThenInclude(s => s.TtsAiConnection),
                    asNoTracking: false,
                    ct: ct
                ) ?? throw new Exception("Pipeline bulunamadı.");

                if (pipeline.ScriptId == null)
                    throw new Exception("Pipeline için script atanmadı.");

                var script = pipeline.Script!;
                var userId = pipeline.AppUserId;

                var dto = JsonSerializer.Deserialize<ScriptContentDto>(script.Content)
                    ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

                // ----------------------------------------
                // Klasörleri oluştur
                // ----------------------------------------
                var appUser = await _appUserRepo.FirstOrDefaultAsync(x => x.Id == userId, ct: ct)
                    ?? throw new Exception("Kullanıcı bulunamadı.");

                var assetRoot = _dir.GetVideoPipelineRoot(appUser, pipeline.Id);
                Directory.CreateDirectory(assetRoot);

                var imageDir = Path.Combine(assetRoot, "images");
                var videoDir = Path.Combine(assetRoot, "videos");
                var audioDir = Path.Combine(assetRoot, "audio");
                var sttDir = Path.Combine(assetRoot, "stt");

                Directory.CreateDirectory(imageDir);
                Directory.CreateDirectory(videoDir);
                Directory.CreateDirectory(audioDir);
                Directory.CreateDirectory(sttDir);

                await Log(pipeline, "Asset üretimi başlatıldı.");

                // ----------------------------------------
                // AI Clientlar
                // ----------------------------------------
                var sgp = pipeline.Profile!.ScriptGenerationProfile!;
                var imageConnId = sgp.ImageAiConnectionId ?? sgp.AiConnectionId;
                var ttsConnId = sgp.TtsAiConnectionId ?? sgp.AiConnectionId;
                var sttConnId = sgp.SttAiConnectionId ?? sgp.AiConnectionId;

                var imageClient = await _aiFactory.ResolveImageClientAsync(userId, imageConnId, ct);
                var ttsClient = await _aiFactory.ResolveTtsClientAsync(userId, ttsConnId, ct);

                var imageModel = sgp.ImageModelName
                    //?? sgp.ImageAiConnection?.ImageModel
                    ?? "imagen-3.0-generate-001";

                var ttsModel = sgp.TtsModelName
                    //?? sgp.TtsAiConnection?.VideoModel
                    ?? "gpt-4o-mini-tts";

                var voiceName = dto.Voice?.Name ?? sgp.TtsVoice ?? "alloy";
                var languageCode = dto.Language ?? "en-US";

                // ----------------------------------------
                // 1) TTS: Script Text → Audio
                // ----------------------------------------
                await _notifier.JobProgressAsync(userId, pipelineId, "🎤 Ses üretiliyor...", 10);

                var audioBytes = await ttsClient.GenerateAudioAsync(
                    dto.ScriptText,
                    voiceName,
                    languageCode,
                    ttsModel,
                    dto.Voice?.Rate ?? "-30%",
                    dto.Voice?.Pitch ?? "+0Hz",
                    "MP3",
                    ct
                );

                var audioPath = Path.Combine(audioDir, "narration.mp3");
                await File.WriteAllBytesAsync(audioPath, audioBytes, ct);

                await SaveAsset(pipeline, AutoVideoAssetFileType.Audio, 0, audioPath.Replace("\\", "/"), "narration");

                // ----------------------------------------
                // 2) STT: Audio → Transcript + Word Timestamps
                // ----------------------------------------
                await _notifier.JobProgressAsync(userId, pipelineId, "📝 STT analizi yapılıyor...", 25);

                var sttClient = await _aiFactory.ResolveSttClientAsync(userId, sttConnId, ct);
                var stt = await sttClient.SpeechToTextAsync(audioBytes, languageCode, null, ct);

                // transcript kaydet
                var transcriptPath = Path.Combine(sttDir, "transcript.txt");
                await File.WriteAllTextAsync(transcriptPath, stt.Transcript, ct);

                // word timestamps kaydet
                var wordsJsonPath = Path.Combine(sttDir, "words.json");
                await File.WriteAllTextAsync(wordsJsonPath,
                    JsonSerializer.Serialize(stt.Words, new JsonSerializerOptions { WriteIndented = true }),
                ct);

                await SaveAsset(pipeline, AutoVideoAssetFileType.Captions, 0, wordsJsonPath.Replace("\\", "/"), "stt_words");

                // ----------------------------------------
                // 3) Görsel Üretimi (Visual Prompts)
                // ----------------------------------------
                await _notifier.JobProgressAsync(userId, pipelineId, "🎨 Görseller üretiliyor...", 40);

                int imgIndex = 0;
                foreach (var vp in dto.VisualPrompts)
                {
                    imgIndex++;

                    var imgBytes = await imageClient.GenerateImageAsync(
                        vp.Prompt,
                        vp.NegativePrompt, // <-- eklendi
                        sgp.ImageAspectRatio ?? "1080x1920",
                        sgp.ImageRenderStyle,
                        imageModel,
                        ct
                    );

                    var filename = $"img_{imgIndex:D3}.jpg";
                    var imgPath = Path.Combine(imageDir, filename);

                    await File.WriteAllBytesAsync(imgPath, imgBytes, ct);

                    await SaveAsset(
                        pipeline,
                        AutoVideoAssetFileType.Image,
                        imgIndex,
                        imgPath.Replace("\\", "/"),
                        filename
                    );
                }

                // ----------------------------------------
                // 4) Video Asset Üretimi (Video Prompts)
                // ----------------------------------------
                //await _notifier.JobProgressAsync(userId, pipelineId, "🎬 Video snippet'lar üretiliyor...", 60);

                //int vidIndex = 0;
                //foreach (var vp in dto.VideoPrompts)
                //{
                //    vidIndex++;

                //    var vidBytes = await imageClient.GenerateVideoAsync(   // özel implement olur
                //        vp.Prompt,
                //        sgp.VideoAspectRatio ?? "1080x1920",
                //        sgp.VideoRenderStyle,
                //        imageModel,
                //        ct
                //    );

                //    var filename = $"vid_{vidIndex:D3}.mp4";
                //    var vidPath = Path.Combine(videoDir, filename);

                //    await File.WriteAllBytesAsync(vidPath, vidBytes, ct);

                //    await SaveAsset(pipeline, AutoVideoAssetFileType.Video, vidIndex, vidPath.Replace("\\", "/"), filename);
                //}

                // ----------------------------------------
                // DTO geri yaz (gerekirse)
                // ----------------------------------------
                script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                _scriptRepo.Update(script);
                await _uow.SaveChangesAsync(ct);

                // ----------------------------------------
                // Tamamlandı
                // ----------------------------------------
                await Log(pipeline, "Asset üretimi tamamlandı.");
                await _notifier.JobCompletedAsync(userId, pipelineId, true, "Asset üretimi tamamlandı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Asset üretimde hata oluştu: " + ex.Message);
                throw;
            }
        }




        // --------------------------------------------------------------------
        // HELPERS
        // --------------------------------------------------------------------

        private async Task SaveAsset(
          ContentPipelineRun_ pipeline,
          AutoVideoAssetFileType type,
          int scene,
          string path,
          string assetKey)
        {
            await _assetRepo.AddAsync(new AutoVideoAssetFile
            {
                AppUserId = pipeline.AppUserId,
                AutoVideoPipelineId = pipeline.Id,
                SceneNumber = scene,
                FileType = type,
                FilePath = path,
                AssetKey = assetKey,
                IsGenerated = true,
            });

            await _uow.SaveChangesAsync();
        }

        private async Task Log(ContentPipelineRun_ p, string msg)
        {
            var logs = string.IsNullOrEmpty(p.LogJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(p.LogJson);

            logs!.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            p.LogJson = JsonSerializer.Serialize(logs);

            _pipelineRepo.Update(p);
            await _uow.SaveChangesAsync();
        }
    }
}
