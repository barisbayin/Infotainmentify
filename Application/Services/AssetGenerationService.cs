using Application.Abstractions;
using Application.AiLayer;
using Application.Contracts.Script;
using Core.Contracts;
using Core.Entity;
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
        private readonly IRepository<AutoVideoPipeline> _pipelineRepo;
        private readonly IRepository<AutoVideoAssetFile> _assetRepo;
        private readonly IRepository<Script> _scriptRepo;
        private readonly IRepository<AppUser> _appUserRepo;
        private readonly IUnitOfWork _uow;
        private readonly IUserDirectoryService _dir;
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly INotifierService _notifier;

        public AssetGenerationService(
            IRepository<AutoVideoPipeline> pipelineRepo,
            IRepository<AutoVideoAssetFile> assetRepo,
            IRepository<Script> scriptRepo,
            IUnitOfWork uow,
            IUserDirectoryService dir,
            IAiGeneratorFactory aiFactory,
            INotifierService notifier)
        {
            _pipelineRepo = pipelineRepo;
            _assetRepo = assetRepo;
            _scriptRepo = scriptRepo;
            _uow = uow;
            _dir = dir;
            _aiFactory = aiFactory;
            _notifier = notifier;
        }

        // --------------------------------------------------------------------
        // MAIN
        // --------------------------------------------------------------------
        public async Task GenerateAssetsAsync(int pipelineId, CancellationToken ct)
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
            // Dizini oluştur
            // ----------------------------------------
            var appUser = await _appUserRepo.FirstOrDefaultAsync(x => x.Id == userId, ct: ct)
                ?? throw new Exception("Kullanıcı bulunamadı.");

            var assetRoot = _dir.GetVideoPipelineRoot(appUser, pipeline.Id);
            Directory.CreateDirectory(assetRoot);

            var imageDir = Path.Combine(assetRoot, "images");
            var audioDir = Path.Combine(assetRoot, "audio");

            Directory.CreateDirectory(imageDir);
            Directory.CreateDirectory(audioDir);

            await Log(pipeline, "Asset üretimi başlatıldı.");

            // ----------------------------------------
            // AI Clientlar
            // ----------------------------------------
            var sgp = pipeline.Profile!.ScriptGenerationProfile!;
            var imageConnId = sgp.ImageAiConnectionId ?? sgp.AiConnectionId;
            var ttsConnId = sgp.TtsAiConnectionId ?? sgp.AiConnectionId;

            var imageClient = await _aiFactory.ResolveImageClientAsync(userId, imageConnId, ct);
            var ttsClient = await _aiFactory.ResolveTtsClientAsync(userId, ttsConnId, ct);

            var imageModel = sgp.ImageModelName
                ?? sgp.ImageAiConnection?.ImageModel
                ?? "imagen-3.0-generate-001";

            var ttsModel = sgp.TtsModelName
                ?? sgp.TtsAiConnection?.VideoModel
                ?? "gpt-4o-mini-tts";

            var voiceName = sgp.TtsVoice
                ?? dto.Voice?.Name
                ?? "alloy";

            int total = dto.Scenes?.Count ?? 0;
            int index = 0;

            foreach (var scene in dto.Scenes)
            {
                index++;
                var progress = (int)((double)index / total * 100);

                // ----------------------------------------
                // Görsel Üret
                // ----------------------------------------
                await _notifier.JobProgressAsync(userId, pipelineId, $"🎨 Görsel üretiliyor (Sahne {scene.Index})", progress);

                if (!string.IsNullOrWhiteSpace(scene.ImagePrompt))
                {
                    var filename = $"scene_{scene.Index:D3}.jpg";
                    var outPath = Path.Combine(imageDir, filename);

                    var bytes = await imageClient.GenerateImageAsync(
                        scene.ImagePrompt,
                        sgp.ImageAspectRatio ?? "1080x1920",
                        sgp.ImageRenderStyle,
                        imageModel,
                        ct
                    );

                    await File.WriteAllBytesAsync(outPath, bytes, ct);
                    scene.ImageGeneratedPath = outPath.Replace("\\", "/");

                    await SaveAsset(pipeline, AutoVideoAssetFileType.Image, scene.Index, scene.ImageGeneratedPath, $"scene_{scene.Index:D3}");
                }

                // ----------------------------------------
                // Ses Üret (Narration)
                // ----------------------------------------
                await _notifier.JobProgressAsync(userId, pipelineId, $"🎤 Ses üretiliyor (Sahne {scene.Index})", progress);

                if (!string.IsNullOrWhiteSpace(scene.Narration))
                {
                    var filename = $"scene_{scene.Index:D3}.mp3";
                    var outPath = Path.Combine(audioDir, filename);

                    var audio = await ttsClient.GenerateAudioAsync(
                        scene.Narration,
                        voiceName,
                        ttsModel,
                        "mp3",
                        ct
                    );

                    await File.WriteAllBytesAsync(outPath, audio, ct);
                    scene.AudioGeneratedPath = outPath.Replace("\\", "/");

                    await SaveAsset(pipeline, AutoVideoAssetFileType.Audio, scene.Index, scene.AudioGeneratedPath, $"scene_{scene.Index:D3}");
                }

                await Task.Delay(1500, ct);
            }

            // JSON geri yaz
            script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            _scriptRepo.Update(script);

            await _uow.SaveChangesAsync(ct);

            await Log(pipeline, "Asset üretimi tamamlandı.");
            await _notifier.JobCompletedAsync(userId, pipelineId, true, "Asset üretimi tamamlandı.");
        }

        // --------------------------------------------------------------------
        // HELPERS
        // --------------------------------------------------------------------

        private async Task SaveAsset(
          AutoVideoPipeline pipeline,
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

        private async Task Log(AutoVideoPipeline p, string msg)
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
