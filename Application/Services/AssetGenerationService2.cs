using Application.Abstractions;
using Application.AiLayer;
using Application.Contracts.Script;
using Core.Contracts;
using Core.Entity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// ScriptContentDto içeriğine bağlı olarak sahne bazlı asset (image/audio/video) üretimi yapan servis.
    /// ScriptGenerationProfile üzerinden ilgili AI bağlantılarını ve model adlarını belirler.
    /// </summary>
    public class AssetGenerationService2
    {
        private readonly IRepository<Script> _scriptRepo;
        private readonly IUnitOfWork _uow;
        private readonly INotifierService _notifier;
        private readonly IUserDirectoryService _dirService;
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IFFmpegService _ffmpeg;
        private readonly IRepository<VideoAsset> _videoAssetRepo;

        public AssetGenerationService2(
            IRepository<Script> scriptRepo,
            IUnitOfWork uow,
            INotifierService notifier,
            IUserDirectoryService dirService,
            IAiGeneratorFactory aiFactory,
            IFFmpegService ffmpeg,
            IRepository<VideoAsset> videoAssetRepo  )
        {
            _scriptRepo = scriptRepo;
            _uow = uow;
            _notifier = notifier;
            _dirService = dirService;
            _aiFactory = aiFactory;
            _ffmpeg = ffmpeg;
            _videoAssetRepo = videoAssetRepo;
        }

        // --------------------------------------------------------------------
        // 🎨 IMAGE GENERATION
        // --------------------------------------------------------------------
        public async Task<string> GenerateImagesAsync(int scriptId, CancellationToken ct = default)
        {
            var script = await _scriptRepo.FirstOrDefaultAsync(
                x => x.Id == scriptId,
                include: q => q
                    .Include(x => x.ScriptGenerationProfile)
                        .ThenInclude(p => p.ImageAiConnection),
                asNoTracking: false,
                ct: ct)
                ?? throw new InvalidOperationException("Script bulunamadı.");

            var profile = script.ScriptGenerationProfile
                ?? throw new InvalidOperationException("ScriptGenerationProfile bulunamadı.");

            var userId = script.UserId;
            var dto = JsonSerializer.Deserialize<ScriptContentDto>(script.Content)
                ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

            // 📁 Kullanıcı dizinleri
            var baseDir = _dirService.GetScriptDirectory(userId, script.Id);
            var imgDir = Path.Combine(baseDir, "images");
            Directory.CreateDirectory(imgDir);

            // 🔹 AI sağlayıcısı profile’dan belirlenir
            var imageConnId = profile.ImageAiConnectionId ?? profile.AiConnectionId;
            var aiClient = await _aiFactory.ResolveImageClientAsync(userId, imageConnId, ct);

            // 🔸 Model fallback zinciri
            string modelName = profile.ImageModelName;
            if (string.IsNullOrWhiteSpace(modelName) && profile.ImageAiConnection != null)
                modelName = profile.ImageAiConnection.ImageModel; // Doğru alan ismi
            modelName ??= "imagen-3.0-generate-001"; // Default model

            int totalScenes = dto.Scenes?.Count ?? 0;
            int processed = 0;

            foreach (var scene in dto.Scenes)
            {
                processed++;
                var progress = (int)((double)processed / totalScenes * 100);

                await _notifier.JobProgressAsync(userId, script.Id,
                    $"🎨 Görsel üretiliyor (Sahne {scene.Index})", progress);

                try
                {
                    var prompt = scene.ImagePrompt;
                    if (string.IsNullOrWhiteSpace(prompt))
                        throw new InvalidOperationException($"Sahne {scene.Index} için imagePrompt eksik.");

                    var filename = $"scene_{scene.Index:D3}.jpg";
                    var outputPath = Path.Combine(imgDir, filename);

                    // 🧠 Görsel verisini al
                    var bytes = await aiClient.GenerateImageAsync(prompt, "1080x1920", profile.ImageRenderStyle, modelName, ct);

                    // 💾 Dosyaya yaz
                    await File.WriteAllBytesAsync(outputPath, bytes, ct);

                    scene.ImageGeneratedPath = outputPath.Replace("\\", "/");
                }
                catch (Exception ex)
                {
                    await _notifier.NotifyUserAsync(userId, "AssetError", new
                    {
                        scene = scene.Index,
                        error = ex.Message
                    });
                }

                await Task.Delay(1500, ct); // Rate limit koruması
            }

            // Güncel JSON’u Script’e yaz
            script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _scriptRepo.Update(script);
            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(userId, script.Id, true,
                $"✅ Görsel üretimi tamamlandı ({processed}/{totalScenes}).");

            return $"Görsel üretimi tamamlandı ({processed}/{totalScenes}).";
        }


        // --------------------------------------------------------------------
        // 🎙️ AUDIO (TTS) GENERATION
        // --------------------------------------------------------------------
        public async Task<string> GenerateAudiosAsync(int scriptId, CancellationToken ct = default)
        {
            var script = await _scriptRepo.FirstOrDefaultAsync(
                x => x.Id == scriptId,
                include: q => q
                    .Include(x => x.ScriptGenerationProfile)
                        .ThenInclude(p => p.TtsAiConnection),
                asNoTracking: false,
                ct: ct)
                ?? throw new InvalidOperationException("Script bulunamadı.");

            var profile = script.ScriptGenerationProfile
                ?? throw new InvalidOperationException("ScriptGenerationProfile bulunamadı.");

            var userId = script.UserId;
            var dto = JsonSerializer.Deserialize<ScriptContentDto>(script.Content)
                ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

            // 📁 Kullanıcı dizinleri
            var baseDir = _dirService.GetScriptDirectory(userId, script.Id);
            var audioDir = Path.Combine(baseDir, "audio");
            Directory.CreateDirectory(audioDir);

            // 🔹 AI sağlayıcısı profile’dan belirlenir
            var ttsConnId = profile.TtsAiConnectionId ?? profile.AiConnectionId;
            var aiClient = await _aiFactory.ResolveTtsClientAsync(userId, ttsConnId, ct);

            // 🔸 Model fallback zinciri
            string modelName = profile.TtsModelName;
            if (string.IsNullOrWhiteSpace(modelName) && profile.TtsAiConnection != null)
                modelName = profile.TtsAiConnection.VideoModel;
            modelName ??= "gpt-4o-mini-tts"; // Default model

            // 🔸 Voice fallback
            string voiceName = profile.TtsVoice
                ?? dto.Voice?.Name
                ?? "alloy";

            int totalScenes = dto.Scenes?.Count ?? 0;
            int processed = 0;

            foreach (var scene in dto.Scenes)
            {
                processed++;
                var progress = (int)((double)processed / totalScenes * 100);

                await _notifier.JobProgressAsync(userId, script.Id,
                    $"🎤 Ses üretiliyor (Sahne {scene.Index})", progress);

                try
                {
                    var narration = scene.Narration;
                    if (string.IsNullOrWhiteSpace(narration))
                        throw new InvalidOperationException($"Sahne {scene.Index} için narration eksik.");

                    var filename = $"scene_{scene.Index:D3}.mp3";
                    var outputPath = Path.Combine(audioDir, filename);

                    // 🎧 AI'dan ses üretimi (ham veriyi al)
                    var audioBytes = await aiClient.GenerateAudioAsync(narration, voiceName, modelName, "mp3", ct);

                    // 💾 Dosyaya yaz
                    await File.WriteAllBytesAsync(outputPath, audioBytes, ct);

                    scene.AudioGeneratedPath = outputPath.Replace("\\", "/");
                }
                catch (Exception ex)
                {
                    await _notifier.NotifyUserAsync(userId, "AssetError", new
                    {
                        scene = scene.Index,
                        error = ex.Message
                    });
                }

                await Task.Delay(1000, ct); // Rate limit koruması
            }

            // Güncel JSON’u Script’e yaz
            script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _scriptRepo.Update(script);
            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(userId, script.Id, true,
                $"✅ Ses üretimi tamamlandı ({processed}/{totalScenes}).");

            return $"Ses üretimi tamamlandı ({processed}/{totalScenes}).";
        }


        public async Task<string> GenerateVideosAsync(int scriptId, CancellationToken ct = default)
        {
            var script = await _scriptRepo.FirstOrDefaultAsync(
                x => x.Id == scriptId,
                asNoTracking: false,
                ct: ct)
                ?? throw new InvalidOperationException("Script bulunamadı.");

            var userId = script.UserId;
            var dto = JsonSerializer.Deserialize<ScriptContentDto>(script.Content)
                ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

            var baseDir = _dirService.GetScriptDirectory(userId, script.Id);
            var renderDir = Path.Combine(baseDir, "renders");
            Directory.CreateDirectory(renderDir);

            var tempVideos = new List<string>();
            int totalScenes = dto.Scenes?.Count ?? 0;
            int processed = 0;

            foreach (var scene in dto.Scenes)
            {
                processed++;
                var progress = (int)((double)processed / totalScenes * 100);

                await _notifier.JobProgressAsync(userId, script.Id,
                    $"🎬 Video render ediliyor (Sahne {scene.Index})", progress);

                try
                {
                    if (string.IsNullOrWhiteSpace(scene.ImageGeneratedPath) ||
                        string.IsNullOrWhiteSpace(scene.AudioGeneratedPath))
                        throw new InvalidOperationException($"Sahne {scene.Index} için eksik medya (görsel veya ses).");

                    var outputPath = Path.Combine(renderDir, $"scene_{scene.Index:D3}.mp4");
                    await _ffmpeg.GenerateSceneVideoAsync(scene.ImageGeneratedPath, scene.AudioGeneratedPath, outputPath, ct);
                    scene.VideoGeneratedPath = outputPath.Replace("\\", "/");
                    tempVideos.Add(outputPath);
                }
                catch (Exception ex)
                {
                    await _notifier.NotifyUserAsync(userId, "RenderError", new
                    {
                        scene = scene.Index,
                        error = ex.Message
                    });
                }

                await Task.Delay(500, ct);
            }

            // Final birleştirme
            var finalPath = Path.Combine(renderDir, $"final_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            await _ffmpeg.ConcatVideosAsync(tempVideos, finalPath, ct);

            // Script güncelle
            script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            _scriptRepo.Update(script);
            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(userId, script.Id, true,
                $"✅ Video render tamamlandı ({processed}/{totalScenes}).");

            return $"Video render tamamlandı ({processed}/{totalScenes}).";
        }

        public async Task<string> GenerateAllAsync(int scriptId, CancellationToken ct = default)
        {
            var script = await _scriptRepo.FirstOrDefaultAsync(
                x => x.Id == scriptId,
                include: q => q
                    .Include(x => x.ScriptGenerationProfile)
                        .ThenInclude(p => p.ImageAiConnection)
                    .Include(x => x.ScriptGenerationProfile)
                        .ThenInclude(p => p.TtsAiConnection),
                asNoTracking: false,
                ct: ct)
                ?? throw new InvalidOperationException("Script bulunamadı.");

            var userId = script.UserId;
            var dto = JsonSerializer.Deserialize<ScriptContentDto>(script.Content)
                ?? throw new InvalidOperationException("Geçersiz Script JSON formatı.");

            await _notifier.JobProgressAsync(userId, script.Id, "🎨 Asset üretimi başlatılıyor...", 0);

            // 1️⃣ Görselleri üret
            await GenerateImagesAsync(scriptId, ct);

            // 2️⃣ Sesleri üret
            await GenerateAudiosAsync(scriptId, ct);

            await _notifier.JobProgressAsync(userId, script.Id, "🎬 Video render başlatılıyor...", 70);

            // 3️⃣ Videoyu oluştur
            var baseDir = _dirService.GetScriptDirectory(userId, script.Id);
            var renderDir = Path.Combine(baseDir, "renders");
            Directory.CreateDirectory(renderDir);

            var sceneVideos = new List<string>();

            foreach (var scene in dto.Scenes)
            {
                if (string.IsNullOrWhiteSpace(scene.ImageGeneratedPath) || string.IsNullOrWhiteSpace(scene.AudioGeneratedPath))
                    continue;

                var sceneVideo = Path.Combine(renderDir, $"scene_{scene.Index:D3}.mp4");

                await _ffmpeg.GenerateSceneVideoAsync(scene.ImageGeneratedPath, scene.AudioGeneratedPath, sceneVideo, ct);

                scene.VideoGeneratedPath = sceneVideo.Replace("\\", "/");

                // 🎥 VideoAsset oluştur
                await _videoAssetRepo.AddAsync(new VideoAsset
                {
                    UserId = userId,
                    ScriptId = script.Id,
                    AssetType = "video",
                    AssetKey = $"scene_{scene.Index:D3}_video",
                    FilePath = scene.VideoGeneratedPath,
                    IsGenerated = true,
                    GeneratedAt = DateTime.Now
                }, ct);

                sceneVideos.Add(sceneVideo);
                await Task.Delay(300, ct);
            }

            // 4️⃣ Final video oluştur
            var finalPath = Path.Combine(renderDir, $"final_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            await _ffmpeg.ConcatVideosAsync(sceneVideos, finalPath, ct);

            // 🎞️ Final video metadata
            var duration = await _ffmpeg.GetVideoDurationAsync(finalPath, ct);
            var thumbPath = await _ffmpeg.GenerateThumbnailAsync(finalPath, renderDir, ct);

            // 🎬 Final VideoAsset
            await _videoAssetRepo.AddAsync(new VideoAsset
            {
                UserId = userId,
                ScriptId = script.Id,
                AssetType = "final",
                AssetKey = "final_render",
                FilePath = finalPath.Replace("\\", "/"),
                IsGenerated = true,
                GeneratedAt = DateTime.Now,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    Duration = duration,
                    Thumbnail = thumbPath
                })
            }, ct);

            // Render bilgilerini dto’ya ekle
            dto.Render = new ScriptRenderInfo
            {
                FilePath = finalPath.Replace("\\", "/"),
                CreatedAt = DateTime.Now,
                DurationSeconds = duration,
                Format = "mp4",
                Resolution = "1080x1920",
                ThumbnailPath = thumbPath.Replace("\\", "/")
            };

            // JSON güncelle
            script.Content = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            _scriptRepo.Update(script);
            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(userId, script.Id, true, "✅ Tüm üretim adımları tamamlandı (Assets + Video).");

            return "Tüm üretim adımları tamamlandı.";
        }

    }
}
