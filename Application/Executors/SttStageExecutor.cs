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

        // Ses süresini ölçmek için bir servis (Yoksa aşağıda basitçe halledeceğiz)
        // private readonly IMediaInfoService _mediaService; 

        public SttStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory)
            : base(sp)
        {
            _aiFactory = aiFactory;
        }

        public override StageType StageType => StageType.Stt;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            var preset = (SttPreset)presetObj!;
            exec.AddLog($"Starting STT with preset: {preset.Name}");

            // 1. TTS Çıktısını Al (Ses dosyalarının yolları lazım)
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
                    exec.AddLog($"Warning: Audio file not found for Scene {audioItem.SceneNumber}");
                    continue;
                }

                exec.AddLog($"Transcribing Scene {audioItem.SceneNumber}...");

                try
                {
                    // Dosyayı oku
                    var audioBytes = await File.ReadAllBytesAsync(audioItem.AudioFilePath, ct);

                    // A) STT Çağrısı (Metni ve zamanları al)
                    var sttResult = await sttClient.SpeechToTextAsync(
                        audioData: audioBytes,
                        languageCode: preset.LanguageCode,
                        model: preset.ModelName,
                        ct: ct
                    );

                    // B) Ses Süresini Bul (Offset için kritik!)
                    double durationSec = GetAudioDuration(audioItem.AudioFilePath);
                    exec.AddLog($"Audio Duration: {durationSec:F2}s");

                    // C) Builder'a Ekle (Hesaplama)
                    subBuilder.AddScene(sttResult.Words, audioItem.SceneNumber, durationSec);

                    successCount++;
                }
                catch (Exception ex)
                {
                    exec.AddLog($"ERROR Scene {audioItem.SceneNumber}: {ex.Message}");
                }

                // Rate limit beklemesi
                await Task.Delay(500, ct);
            }

            if (successCount == 0)
                throw new Exception("Hiçbir altyazı üretilemedi.");

            var finalSubtitles = subBuilder.Build();
            exec.AddLog($"STT Completed. Total Words: {finalSubtitles.Count}");

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
                // YÖNTEM 1: NAudio (Eğer paket yüklüyse bunu aç)

                using var reader = new AudioFileReader(filePath);
                return reader.TotalTime.TotalSeconds;


                // YÖNTEM 2: Dosya boyutundan tahmin (MP3 için kaba hesap)
                // 1 saniye ~ 16KB (128kbps mono için). Çok güvenilir değildir ama iş görür.
                // Gerçek projede FFmpeg veya NAudio şart.
                //var info = new FileInfo(filePath);
                // 128kbps = 16000 bytes/sec yaklaşık
                //return info.Length / 16000.0;
            }
            catch
            {
                // Fallback: 5 saniye varsayalım (Render'da düzeltilir)
                return 5.0;
            }
        }
    }
}
