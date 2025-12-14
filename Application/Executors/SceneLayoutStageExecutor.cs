using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
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
            await logAsync("📐 Building Scene Layout (Timeline)...");

            // 1. Collect Inputs
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            var imageData = context.GetOutput<ImageStagePayload>(StageType.Image);
            var ttsData = context.GetOutput<TtsStagePayload>(StageType.Tts);

            // STT is Optional
            SttStagePayload? sttData = null;
            try { sttData = context.GetOutput<SttStagePayload>(StageType.Stt); } catch { }

            // 2. Load Render Settings (Preset)
            if (!config.PresetId.HasValue)
                throw new InvalidOperationException("No Render Preset selected for SceneLayout.");

            var renderPreset = await _renderPresetRepo.GetByIdAsync(config.PresetId.Value, true, ct);
            if (renderPreset == null)
                throw new InvalidOperationException("Render Preset not found.");

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
                    BitrateKbps = renderPreset.BitrateKbps,
                    EncoderPreset = renderPreset.EncoderPreset,
                    FontSize = capSettings.FontSize > 0 ? capSettings.FontSize : 30,
                    MusicVolume = (audioSettings.MusicVolumePercent > 0 ? audioSettings.MusicVolumePercent : 15) / 100.0,
                    IsDuckingEnabled = audioSettings.EnableDucking
                }
            };

            double currentTime = 0;
            var sceneCount = scriptData.Scenes.Count;

            await logAsync($"Processing {sceneCount} scenes for the timeline...");

            for (int i = 0; i < sceneCount; i++)
            {
                var sceneNum = i + 1;

                var img = imageData.SceneImages.FirstOrDefault(x => x.SceneNumber == sceneNum);
                var aud = ttsData.SceneAudios.FirstOrDefault(x => x.SceneNumber == sceneNum);

                // 🔥 1. AUDIO CHECK
                if (aud == null)
                {
                    await logAsync($"⚠️ Warning: Scene {sceneNum} audio missing. Skipping.");
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
                    await logAsync($"⚠️ Warning: Scene {sceneNum} image missing. Applying fallback.");

                    // Plan A: Use previous image
                    if (layout.VisualTrack.Count > 0)
                    {
                        currentImagePath = layout.VisualTrack.Last().ImagePath;
                    }
                    else
                    {
                        // Plan B: Default black background for the first scene
                        currentImagePath = _dir.GetDefaultBlackBackground();
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
                    await logAsync($"⚠️ Warning: Could not read audio duration for Scene {sceneNum}. Defaulting to 5s.");
                }

                double sceneDuration = exactAudioDuration;

                // A) Visual Track
                layout.VisualTrack.Add(new VisualEvent
                {
                    SceneIndex = sceneNum,
                    ImagePath = currentImagePath,
                    StartTime = currentTime,
                    Duration = sceneDuration,
                    EffectType = i % 2 == 0 ? "zoom_in" : "zoom_out",
                    ZoomIntensity = visualSettings.ZoomIntensity,
                    TransitionType = visualSettings.TransitionType,
                    TransitionDuration = visualSettings.TransitionDurationSec
                });

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

            await logAsync($"✅ Timeline created successfully. Total Duration: {layout.TotalDuration:F1}s");

            return layout;
        }
    }
}
