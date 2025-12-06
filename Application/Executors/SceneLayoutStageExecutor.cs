using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.SceneLayout)]
    // Bu aşamanın kendine ait bir Preset tablosu yok, RenderPreset kullanır.
    // Ancak StageConfig'de PresetId genellikle RenderPresetId'ye işaret eder.
    public class SceneLayoutStageExecutor : BaseStageExecutor
    {
        private readonly IRepository<RenderPreset> _renderPresetRepo;

        public SceneLayoutStageExecutor(
            IServiceProvider sp,
            IRepository<RenderPreset> renderPresetRepo)
            : base(sp)
        {
            _renderPresetRepo = renderPresetRepo;
        }

        public override StageType StageType => StageType.SceneLayout;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj, // Burası null gelebilir, manuel çekeceğiz
            CancellationToken ct)
        {
            exec.AddLog("Building Scene Layout (Timeline)...");

            // 1. Girdileri Topla
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            var imageData = context.GetOutput<ImageStagePayload>(StageType.Image);
            var ttsData = context.GetOutput<TtsStagePayload>(StageType.Tts);

            // STT Opsiyonel (Altyazı istenmemiş olabilir)
            SttStagePayload? sttData = null;
            try { sttData = context.GetOutput<SttStagePayload>(StageType.Stt); } catch { }

            // 2. Render Ayarlarını (Preset) Çek
            // StageConfig'deki PresetId, RenderPreset tablosunu işaret ediyor olmalı.
            if (!config.PresetId.HasValue)
                throw new InvalidOperationException("SceneLayout için bir Render Preset seçilmemiş.");

            var renderPreset = await _renderPresetRepo.GetByIdAsync(config.PresetId.Value, true, ct);
            if (renderPreset == null)
                throw new InvalidOperationException("Render Preset bulunamadı.");

            // JSON Ayarlarını Deserialize Et (Entity içindeki helper property ile)
            var visualSettings = renderPreset.VisualEffectsSettings;
            var audioSettings = renderPreset.AudioMixSettings;

            // 3. Timeline Oluşturma
            var layout = new SceneLayoutStagePayload
            {
                Width = renderPreset.OutputWidth,
                Height = renderPreset.OutputHeight,
                Fps = renderPreset.Fps,
                TotalDuration = 0
            };

            double currentTime = 0;

            // Sahneleri Eşleştir ve Sırala
            var sceneCount = scriptData.Scenes.Count;

            for (int i = 0; i < sceneCount; i++)
            {
                var sceneNum = i + 1;

                // İlgili dosyaları bul
                var img = imageData.SceneImages.FirstOrDefault(x => x.SceneNumber == sceneNum);
                var aud = ttsData.SceneAudios.FirstOrDefault(x => x.SceneNumber == sceneNum);

                if (img == null || aud == null)
                {
                    exec.AddLog($"Warning: Scene {sceneNum} missing image or audio. Skipping.");
                    continue;
                }

                // Süreyi belirle (Ses dosyası süresi esastır)
                // Not: TtsStageExecutor'da NAudio ile süreyi hesaplayıp SceneAudioItem'a ekleseydik harika olurdu.
                // Şimdilik STT verisinden veya varsayılan bir değerden alalım.
                double duration = 5.0; // Fallback

                // En doğrusu: STT verisindeki son kelimenin bitiş süresi - ilk kelimenin başlangıç süresi? 
                // Hayır, ses dosyasının fiziksel süresi lazım. 
                // TtsExecutor'a "DurationSec" alanı ekleyip oradan okumak en temizidir.
                // Şimdilik basitçe script'teki tahmini süreyi kullanalım, Render aşamasında FFmpeg gerçek süreyi ölçecek.
                duration = scriptData.Scenes.FirstOrDefault(s => s.SceneNumber == sceneNum)?.EstimatedDuration ?? 5;

                // A) Görsel Kanalı (Visual Track)
                layout.VisualTrack.Add(new VisualEvent
                {
                    SceneIndex = sceneNum,
                    ImagePath = img.ImagePath,
                    StartTime = currentTime,
                    Duration = duration,
                    EffectType = i % 2 == 0 ? "zoom_in" : "zoom_out", // Sırayla efekt değiştir
                    ZoomIntensity = visualSettings.ZoomIntensity,
                    TransitionType = visualSettings.TransitionType,
                    TransitionDuration = visualSettings.TransitionDurationSec
                });

                // B) Ses Kanalı (Voice Track)
                layout.AudioTrack.Add(new AudioEvent
                {
                    Type = "voice",
                    FilePath = aud.AudioFilePath,
                    StartTime = currentTime,
                    Volume = audioSettings.VoiceVolumePercent / 100.0
                });

                // C) Altyazı Kanalı (Caption Track) - Offsetli
                if (sttData != null)
                {
                    var sceneWords = sttData.Subtitles
                        .Where(w => w.SceneNumber == sceneNum)
                        .Select(w => new CaptionEvent
                        {
                            Text = w.Word,
                            Start = w.Start + currentTime, // Global zamana göre kaydır
                            End = w.End + currentTime
                        });

                    layout.CaptionTrack.AddRange(sceneWords);
                }

                // Sayacı ilerlet
                currentTime += duration;
            }

            layout.TotalDuration = currentTime;

            // D) Arka Plan Müziği (Opsiyonel)
            // Eğer Preset'te bir müzik yolu varsa buraya eklenir.
            // layout.AudioTrack.Add(new AudioEvent { Type = "music", ... });

            exec.AddLog($"Timeline created. Total Duration: {layout.TotalDuration:F1}s");

            return layout;
        }
    }
}
