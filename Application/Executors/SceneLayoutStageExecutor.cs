using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity.Models;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using NAudio.Wave;

namespace Application.Executors
{
    [StageExecutor(StageType.SceneLayout)]
    // This stage uses RenderPreset for configuration
    public class SceneLayoutStageExecutor : BaseStageExecutor
    {
        private readonly IRepository<RenderPreset> _renderPresetRepo;
        private readonly IUserDirectoryService _dir;

        public SceneLayoutStageExecutor(
            IServiceProvider sp,
            IRepository<RenderPreset> renderPresetRepo,
            IUserDirectoryService userDirectoryService)
            : base(sp)
        {
            _renderPresetRepo = renderPresetRepo;
            _dir = userDirectoryService;
        }

        public override StageType StageType => StageType.SceneLayout;

        // 🔥 FIX 1: Changed to 'protected override' and added 'logAsync' parameter
        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync, // <--- Live Logging Function
            CancellationToken ct)
        {
            // 🔥 FIX 2: Using logAsync instead of exec.AddLog
            await logAsync("Kurgu planı oluşturuluyor. Script, görseller ve sesler timeline'a bağlanacak.");

            // 1. Collect Inputs
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            var imageData = context.GetOutput<ImageStagePayload>(StageType.Image);
            var ttsData = context.GetOutput<TtsStagePayload>(StageType.Tts);

            // STT is Optional
            SttStagePayload? sttData = null;
            try { sttData = context.GetOutput<SttStagePayload>(StageType.Stt); } catch { }

            // 2. Load Render Settings (Preset)
            if (!config.PresetId.HasValue)
                throw new InvalidOperationException("SceneLayout için Render preset seçilmemiş.");

            var renderPreset = await _renderPresetRepo.GetByIdAsync(config.PresetId.Value, true, ct);
            if (renderPreset == null)
                throw new InvalidOperationException("Render preset bulunamadı.");

            // Deserialize JSON Settings
            var visualSettings = renderPreset.VisualEffectsSettings;
            var audioSettings = renderPreset.AudioMixSettings;
            var capSettings = renderPreset.CaptionSettings;

            // 3. Initialize Timeline
            var layout = new SceneLayoutStagePayload
            {
                Width = renderPreset.OutputWidth,
                Height = renderPreset.OutputHeight,
                Fps = renderPreset.Fps,
                TotalDuration = 0,
                // Style Settings
                Style = new RenderStyleSettings
                {
                    // Teknik
                    BitrateKbps = renderPreset.BitrateKbps,
                    EncoderPreset = renderPreset.EncoderPreset,

                    // Gruplar (Helper property'ler sayesinde JSON parse edilmiş olarak gelir)
                    CaptionSettings = renderPreset.CaptionSettings,
                    AudioMixSettings = renderPreset.AudioMixSettings,
                    VisualEffectsSettings = renderPreset.VisualEffectsSettings,
                    BrandingSettings = renderPreset.BrandingSettings
                }
            };

            double currentTime = 0;
            var sceneCount = scriptData.Scenes.Count;

            await logAsync($"Timeline için işlenecek sahne sayısı: {sceneCount}. Çıkış: {layout.Width}x{layout.Height}, FPS: {layout.Fps}.");

            for (int i = 0; i < sceneCount; i++)
            {
                var sceneNum = i + 1;

                var img = imageData.SceneImages.FirstOrDefault(x => x.SceneNumber == sceneNum);
                var aud = ttsData.SceneAudios.FirstOrDefault(x => x.SceneNumber == sceneNum);

                // 🔥 1. AUDIO CHECK
                if (aud == null)
                {
                    await logAsync(PipelineLiveLog.Warning($"Sahne {sceneNum} için ses bulunamadı. Sahne timeline'a eklenmeyecek."));
                    continue;
                }

                // 🔥 2. IMAGE CHECK & FALLBACK
                string currentImagePath;

                if (img != null)
                {
                    currentImagePath = img.ImagePath;
                }
                else
                {
                    await logAsync(PipelineLiveLog.Warning($"Sahne {sceneNum} için görsel bulunamadı. Fallback görsel kullanılacak."));

                    // Plan A: Use previous image
                    if (layout.VisualTrack.Count > 0)
                    {
                        currentImagePath = layout.VisualTrack.Last().ImagePath;
                    }
                    else
                    {
                        // Plan B: Default black background for the first scene
                        currentImagePath = await _dir.GetDefaultBlackBackground();
                    }
                }

                // Calculate Duration from Audio File
                double exactAudioDuration = 0;

                try
                {
                    using (var audioReader = new AudioFileReader(aud.AudioFilePath))
                    {
                        exactAudioDuration = audioReader.TotalTime.TotalSeconds;
                    }
                }
                catch
                {
                    exactAudioDuration = 5.0; // Fallback duration
                    await logAsync(PipelineLiveLog.Warning($"Sahne {sceneNum} ses süresi okunamadı. Varsayılan süre 5 sn kullanılacak."));
                }

                double sceneDuration = exactAudioDuration;

                // A) Visual Track
                var visualEvents = BuildVisualEventsForScene(
                    sceneNum,
                    currentImagePath,
                    currentTime,
                    sceneDuration,
                    visualSettings,
                    i);

                layout.VisualTrack.AddRange(visualEvents);

                if (visualEvents.Count > 1)
                {
                    await logAsync($"Sahne {sceneNum} için otomatik B-roll planlandı. Görsel vuruş: {visualEvents.Count}, süre: {sceneDuration:F1} sn.");
                }

                // B) Voice Track
                layout.AudioTrack.Add(new AudioEvent
                {
                    Type = "voice",
                    FilePath = aud.AudioFilePath,
                    StartTime = currentTime,
                    Volume = audioSettings.VoiceVolumePercent / 100.0
                });

                // C) Caption Track
                if (sttData != null)
                {
                    var sceneWords = sttData.Subtitles
                        .Where(w => w.SceneNumber == sceneNum)
                        .Select(w => new CaptionEvent
                        {
                            Text = w.Word,
                            Start = w.Start, // Absolute time from STT
                            End = w.End      // Absolute time from STT
                        });

                    layout.CaptionTrack.AddRange(sceneWords);
                }

                currentTime += sceneDuration;
            }

            layout.TotalDuration = currentTime;

            await logAsync(PipelineLiveLog.Success($"Kurgu planı oluşturuldu. Toplam süre: {layout.TotalDuration:F1} sn, görsel vuruş: {layout.VisualTrack.Count}."));

            return layout;
        }

        private static List<VisualEvent> BuildVisualEventsForScene(
            int sceneNumber,
            string imagePath,
            double sceneStartTime,
            double sceneDuration,
            RenderVisualEffectsSettings visualSettings,
            int sceneIndexZeroBased)
        {
            var defaultEffect = sceneIndexZeroBased % 2 == 0 ? "zoom_in" : "zoom_out";

            if (!visualSettings.EnableAutoBroll)
            {
                return new List<VisualEvent>
                {
                    CreateVisualEvent(sceneNumber, imagePath, sceneStartTime, sceneDuration, visualSettings, defaultEffect, "primary", 1, 1)
                };
            }

            var minSceneDuration = Math.Max(6, visualSettings.MinSceneDurationForBrollSec);
            var segmentDuration = Math.Max(4, visualSettings.BrollSegmentDurationSec);
            var maxSegments = Math.Clamp(visualSettings.MaxBrollCutsPerScene, 2, 12);

            if (sceneDuration < minSceneDuration || sceneDuration <= segmentDuration + 2)
            {
                return new List<VisualEvent>
                {
                    CreateVisualEvent(sceneNumber, imagePath, sceneStartTime, sceneDuration, visualSettings, defaultEffect, "primary", 1, 1)
                };
            }

            var segmentCount = Math.Clamp((int)Math.Ceiling(sceneDuration / segmentDuration), 2, maxSegments);
            var actualSegmentDuration = sceneDuration / segmentCount;
            var effects = new[] { "zoom_in", "pan_left", "zoom_out", "pan_right", "pan_up", "pan_down" };
            var events = new List<VisualEvent>(segmentCount);

            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var start = sceneStartTime + segmentIndex * actualSegmentDuration;
                var duration = segmentIndex == segmentCount - 1
                    ? sceneDuration - actualSegmentDuration * segmentIndex
                    : actualSegmentDuration;
                var effect = effects[(sceneIndexZeroBased + segmentIndex) % effects.Length];

                events.Add(CreateVisualEvent(
                    sceneNumber,
                    imagePath,
                    start,
                    Math.Max(0.25, duration),
                    visualSettings,
                    effect,
                    segmentIndex == 0 ? "primary" : "broll_auto",
                    segmentIndex + 1,
                    segmentCount));
            }

            return events;
        }

        private static VisualEvent CreateVisualEvent(
            int sceneNumber,
            string imagePath,
            double startTime,
            double duration,
            RenderVisualEffectsSettings visualSettings,
            string effectType,
            string visualRole,
            int segmentIndex,
            int segmentCount)
        {
            return new VisualEvent
            {
                SceneIndex = sceneNumber,
                ImagePath = imagePath,
                StartTime = startTime,
                Duration = duration,
                EffectType = effectType,
                ZoomIntensity = visualSettings.ZoomIntensity,
                TransitionType = visualSettings.TransitionType,
                TransitionDuration = visualSettings.TransitionDurationSec,
                VisualRole = visualRole,
                SegmentIndex = segmentIndex,
                SegmentCount = segmentCount
            };
        }
    }
}
