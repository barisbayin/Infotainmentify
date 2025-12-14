using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.Tts)]
    [StagePreset(typeof(TtsPreset))]
    public class TtsStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IUserDirectoryService _dirService;

        public TtsStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IUserDirectoryService dirService)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _dirService = dirService;
        }

        public override StageType StageType => StageType.Tts;

        // 🔥 DÜZELTME 1: 'protected override' yaptık ve logAsync'i ekledik
        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync, // 🔥 Canlı Log Fonksiyonu
            CancellationToken ct)
        {
            var preset = (TtsPreset)presetObj!;

            // 🔥 DÜZELTME 2: exec.AddLog -> logAsync
            await logAsync($"🗣️ Starting TTS (Text-to-Speech) with preset: {preset.Name} ({preset.VoiceId})");

            // 1. Script Verisini Çek
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            if (scriptData == null || scriptData.Scenes == null || !scriptData.Scenes.Any())
                throw new InvalidOperationException("Script verisi bulunamadı.");

            // 2. AI İstemcisi
            var ttsClient = await _aiFactory.ResolveTtsClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 3. Klasör Hazırla
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "audio");

            var results = new List<SceneAudioItem>();
            int successCount = 0;

            await logAsync($"Found {scriptData.Scenes.Count} scenes to synthesize.");

            // 4. Döngü: Her sahne için ses üret
            foreach (var scene in scriptData.Scenes)
            {
                if (ct.IsCancellationRequested) break;

                // Eğer seslendirilecek metin yoksa atla
                if (string.IsNullOrWhiteSpace(scene.AudioText))
                {
                    await logAsync($"⚠️ Scene {scene.SceneNumber}: No audio text, skipping.");
                    continue;
                }

                await logAsync($"🔊 Generating audio for Scene {scene.SceneNumber} ({scene.AudioText.Length} chars)...");

                try
                {
                    // AI Çağrısı (Byte Array döner)
                    var audioBytes = await ttsClient.GenerateAudioAsync(
                        text: scene.AudioText,
                        voiceName: preset.VoiceId,
                        languageCode: preset.LanguageCode,
                        modelName: preset.EngineModel ?? "",
                        ratePercent: FormatRate(preset.SpeakingRate),
                        pitchString: FormatPitch(preset.Pitch),
                        audioEncoding: "MP3",
                        ct: ct
                    );

                    // Kaydet
                    var fileName = $"scene_{scene.SceneNumber:00}_{Guid.NewGuid().ToString()[..6]}.mp3";
                    var fullPath = Path.Combine(outputDir, fileName);

                    await File.WriteAllBytesAsync(fullPath, audioBytes, ct);

                    results.Add(new SceneAudioItem
                    {
                        SceneNumber = scene.SceneNumber,
                        AudioFilePath = fullPath,
                        TextSpoken = scene.AudioText
                    });

                    successCount++;
                    await logAsync($"✅ Scene {scene.SceneNumber} audio ready.");
                }
                catch (Exception ex)
                {
                    // Hata logu
                    await logAsync($"❌ ERROR Scene {scene.SceneNumber}: {ex.Message}");
                    // Hata olsa da devam et (Diğer sahneler üretilsin)
                }

                // Rate limit koruması
                await Task.Delay(500, ct);
            }

            if (successCount == 0)
                throw new Exception("Hiçbir ses dosyası üretilemedi.");

            await logAsync($"🎉 TTS Completed. Total Files: {successCount}");

            return new TtsStagePayload
            {
                ScriptId = scriptData.ScriptId,
                SceneAudios = results
            };
        }

        // --- Helpers ---
        private string FormatRate(double rate) => rate.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        private string FormatPitch(double pitch) => pitch.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
