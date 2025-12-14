using Application.AiLayer.Abstract;
using Application.Helpers;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using NAudio.Wave;

namespace Application.Executors
{
    [StageExecutor(StageType.Stt)]
    [StagePreset(typeof(SttPreset))]
    public class SttStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;

        public SttStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory)
            : base(sp)
        {
            _aiFactory = aiFactory;
        }

        public override StageType StageType => StageType.Stt;

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
            var preset = (SttPreset)presetObj!;

            // 🔥 DÜZELTME 2: exec.AddLog -> logAsync
            await logAsync($"🎧 Starting STT (Speech-to-Text) with preset: {preset.Name}");

            // 1. TTS Çıktısını Al
            var ttsPayload = context.GetOutput<TtsStagePayload>(StageType.Tts);
            if (ttsPayload == null || !ttsPayload.SceneAudios.Any())
                throw new InvalidOperationException("TTS verisi bulunamadı. Önce ses üretmelisiniz.");

            // 2. AI Client
            var sttClient = await _aiFactory.ResolveSttClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 3. Builder Başlat
            var subBuilder = new SubtitleBuilder();
            int successCount = 0;

            // 4. Döngü
            foreach (var audioItem in ttsPayload.SceneAudios.OrderBy(x => x.SceneNumber))
            {
                if (ct.IsCancellationRequested) break;

                if (!File.Exists(audioItem.AudioFilePath))
                {
                    await logAsync($"⚠️ Warning: Audio file not found for Scene {audioItem.SceneNumber}");
                    continue;
                }

                await logAsync($"🗣️ Transcribing Scene {audioItem.SceneNumber}...");

                try
                {
                    // Dosyayı oku
                    var audioBytes = await File.ReadAllBytesAsync(audioItem.AudioFilePath, ct);

                    // A) STT Çağrısı
                    var sttResult = await sttClient.SpeechToTextAsync(
                        audioData: audioBytes,
                        languageCode: preset.LanguageCode,
                        model: preset.ModelName,
                        ct: ct
                    );

                    // B) Ses Süresini Bul
                    double durationSec = GetAudioDuration(audioItem.AudioFilePath);

                    // C) Builder'a Ekle
                    // (Önceki sahnelerin sürelerini toplayarak offset ekleyen bir yapı)
                    subBuilder.AddScene(sttResult.Words, audioItem.SceneNumber, durationSec);

                    successCount++;
                    // Log
                    await logAsync($"✅ Scene {audioItem.SceneNumber} transcribed. Words: {sttResult.Words.Count}");
                }
                catch (Exception ex)
                {
                    // Hata logu
                    await logAsync($"❌ ERROR Scene {audioItem.SceneNumber}: {ex.Message}");
                }

                // Rate limit
                await Task.Delay(500, ct);
            }

            if (successCount == 0)
                throw new Exception("Hiçbir altyazı üretilemedi.");

            var finalSubtitles = subBuilder.Build();
            await logAsync($"🎉 STT Completed. Total Words: {finalSubtitles.Count}");

            return new SttStagePayload
            {
                ScriptId = ttsPayload.ScriptId,
                Subtitles = finalSubtitles
            };
        }

        // --- Helper: Ses Süresi Ölçer ---
        private double GetAudioDuration(string filePath)
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                return reader.TotalTime.TotalSeconds;
            }
            catch
            {
                // Fallback
                return 5.0;
            }
        }
    }
}
