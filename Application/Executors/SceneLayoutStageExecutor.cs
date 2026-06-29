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

            // EditPlan is Optional. When present, it drives beat timing and editor decisions.
            EditPlanStagePayload? editPlan = null;
            if (context.HasOutput(StageType.EditPlan))
            {
                editPlan = context.GetOutput<EditPlanStagePayload>(StageType.EditPlan);
                await logAsync($"EditPlan aktif. Kurgu kararları timeline'a uygulanacak. Sahne planı: {editPlan.Scenes.Count}.");
            }

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

                var sceneImages = imageData.SceneImages
                    .Where(x => x.SceneNumber == sceneNum)
                    .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                    .ToList();
                var aud = ttsData.SceneAudios.FirstOrDefault(x => x.SceneNumber == sceneNum);

                // 🔥 1. AUDIO CHECK
                if (aud == null)
                {
                    await logAsync(PipelineLiveLog.Warning($"Sahne {sceneNum} için ses bulunamadı. Sahne timeline'a eklenmeyecek."));
                    continue;
                }

                // 🔥 2. IMAGE CHECK & FALLBACK
                if (sceneImages.Count == 0)
                {
                    await logAsync(PipelineLiveLog.Warning($"Sahne {sceneNum} için görsel bulunamadı. Fallback görsel kullanılacak."));

                    // Plan A: Use previous image
                    if (layout.VisualTrack.Count > 0)
                    {
                        sceneImages.Add(new SceneImageItem
                        {
                            SceneNumber = sceneNum,
                            BeatIndex = 1,
                            BeatCount = 1,
                            BeatRole = "fallback_previous",
                            EffectType = "slow_push_in",
                            TransitionType = visualSettings.TransitionType,
                            ImagePath = layout.VisualTrack.Last().ImagePath,
                            PromptUsed = "Previous scene fallback"
                        });
                    }
                    else
                    {
                        // Plan B: Default black background for the first scene
                        sceneImages.Add(new SceneImageItem
                        {
                            SceneNumber = sceneNum,
                            BeatIndex = 1,
                            BeatCount = 1,
                            BeatRole = "fallback",
                            EffectType = "static",
                            TransitionType = visualSettings.TransitionType,
                            ImagePath = await _dir.GetDefaultBlackBackground(),
                            PromptUsed = "Default black background"
                        });
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
                var editScene = editPlan?.Scenes.FirstOrDefault(x => x.SceneNumber == sceneNum);
                var visualEvents = editScene?.VisualBeats.Count > 0
                    ? BuildEditPlanVisualEventsForScene(sceneNum, sceneImages, editScene, currentTime, sceneDuration, visualSettings)
                    : sceneImages.Count > 1
                        ? BuildStoryboardVisualEventsForScene(sceneNum, sceneImages, currentTime, sceneDuration, visualSettings)
                        : BuildVisualEventsForScene(
                            sceneNum,
                            sceneImages[0].ImagePath,
                            currentTime,
                            sceneDuration,
                            visualSettings,
                            i,
                            sceneImages[0]);

                layout.VisualTrack.AddRange(visualEvents);

                if (editScene?.VisualBeats.Count > 0)
                {
                    await logAsync($"Sahne {sceneNum} EditPlan ile timeline'a eklendi. Pacing: {editScene.Pacing}, beat: {visualEvents.Count}, süre: {sceneDuration:F1} sn.");
                }
                else if (sceneImages.Count > 1)
                {
                    await logAsync($"Sahne {sceneNum} storyboard ritmiyle timeline'a eklendi. Beat sayısı: {sceneImages.Count}, süre: {sceneDuration:F1} sn.");
                }
                else if (visualEvents.Count > 1)
                {
                    await logAsync($"Sahne {sceneNum} için otomatik B-roll planlandı. Görsel vuruş: {visualEvents.Count}, süre: {sceneDuration:F1} sn.");
                }

                // B) Voice Track
                var audioEdit = ResolveAudioEditDecision(editScene, visualEvents, i, sceneDuration, audioSettings);
                var voiceStartTime = Math.Max(0, currentTime + audioEdit.OffsetSec);
                var actualAudioOffset = voiceStartTime - currentTime;
                foreach (var visual in visualEvents)
                {
                    visual.AudioTransition = audioEdit.Type;
                    visual.AudioOffsetSec = actualAudioOffset;
                }

                layout.AudioTrack.Add(new AudioEvent
                {
                    Type = "voice",
                    FilePath = aud.AudioFilePath,
                    SceneNumber = sceneNum,
                    StartTime = voiceStartTime,
                    Duration = sceneDuration,
                    Volume = audioSettings.VoiceVolumePercent / 100.0,
                    FadeInSec = audioEdit.FadeInSec,
                    FadeOutSec = audioEdit.FadeOutSec,
                    EditTransition = audioEdit.Type,
                    EditOffsetSec = actualAudioOffset
                });

                if (audioEdit.Type != "straight")
                {
                    await logAsync($"Sahne {sceneNum} ses geçişi: {FormatAudioEditName(audioEdit.Type)} ({actualAudioOffset:+0.00;-0.00;0.00} sn).");
                }

                if (editScene?.AudioCues.Count > 0)
                {
                    var addedCueCount = await AddAudioCuesAsync(layout, editScene, currentTime, sceneDuration, audioSettings, logAsync);
                    if (addedCueCount > 0)
                    {
                        await logAsync($"Sahne {sceneNum} için {addedCueCount} audio cue timeline'a eklendi.");
                    }
                }

                // C) Caption Track
                if (sttData != null)
                {
                    var sceneWords = sttData.Subtitles
                        .Where(w => w.SceneNumber == sceneNum)
                        .Select(w => new CaptionEvent
                        {
                            Text = w.Word,
                            Start = Math.Max(0, w.Start + actualAudioOffset),
                            End = Math.Max(Math.Max(0, w.Start + actualAudioOffset) + 0.04, w.End + actualAudioOffset)
                        });

                    layout.CaptionTrack.AddRange(sceneWords);
                }

                currentTime += sceneDuration;
            }

            layout.TotalDuration = currentTime;
            layout.EditDecisionList = BuildEditDecisionList(layout.VisualTrack, layout.AudioTrack);
            layout.BrollLayerPlan = BuildBrollLayerPlan(layout.VisualTrack);
            layout.ReviewReport = BuildReviewReport(layout, sceneCount);

            await logAsync(PipelineLiveLog.Success($"Kurgu planı oluşturuldu. Toplam süre: {layout.TotalDuration:F1} sn, görsel vuruş: {layout.VisualTrack.Count}, EDL karar: {layout.EditDecisionList.Count}."));
            if (layout.BrollLayerPlan.Count > 0)
            {
                await logAsync($"B-roll / bilgi görseli layer planı hazır. Vuruş: {layout.BrollLayerPlan.Count}.");
            }
            await logAsync($"Render öncesi kontrol: {FormatReviewStatus(layout.ReviewReport.Status)}. Hata: {layout.ReviewReport.ErrorCount}, uyarı: {layout.ReviewReport.WarningCount}, bilgi: {layout.ReviewReport.InfoCount}.");

            return layout;
        }

        private static List<VisualEvent> BuildVisualEventsForScene(
            int sceneNumber,
            string imagePath,
            double sceneStartTime,
            double sceneDuration,
            RenderVisualEffectsSettings visualSettings,
            int sceneIndexZeroBased,
            SceneImageItem? imageItem = null)
        {
            var defaultEffect = NormalizeEffectType(imageItem?.EffectType);
            if (string.IsNullOrWhiteSpace(defaultEffect))
                defaultEffect = sceneIndexZeroBased % 2 == 0 ? "zoom_in" : "zoom_out";

            if (!visualSettings.EnableAutoBroll)
            {
                return new List<VisualEvent>
                {
                    CreateVisualEvent(
                        sceneNumber,
                        imagePath,
                        sceneStartTime,
                        sceneDuration,
                        visualSettings,
                        defaultEffect,
                        imageItem?.BeatRole ?? "primary",
                        1,
                        1,
                        imageItem?.TransitionType,
                        overlayText: imageItem?.OverlayText,
                        shotType: imageItem?.ShotType,
                        directorIntent: imageItem?.DirectorIntent,
                        continuityAnchor: imageItem?.ContinuityAnchor,
                        composition: imageItem?.Composition,
                        visualType: imageItem?.VisualType,
                        varietyRole: imageItem?.VarietyRole,
                        varietyReason: imageItem?.VarietyReason,
                        visualQualityScore: imageItem?.VisualQualityScore,
                        visualQualityNotes: imageItem?.VisualQualityNotes,
                        sourceImageSceneNumber: GetSourceImageSceneNumber(sceneNumber, imageItem),
                        sourceImageBeatIndex: imageItem?.BeatIndex,
                        isFallbackImage: IsFallbackImage(sceneNumber, imageItem))
                };
            }

            var minSceneDuration = Math.Max(6, visualSettings.MinSceneDurationForBrollSec);
            var segmentDuration = Math.Max(4, visualSettings.BrollSegmentDurationSec);
            var maxSegments = Math.Clamp(visualSettings.MaxBrollCutsPerScene, 2, 12);

            if (sceneDuration < minSceneDuration || sceneDuration <= segmentDuration + 2)
            {
                return new List<VisualEvent>
                {
                    CreateVisualEvent(
                        sceneNumber,
                        imagePath,
                        sceneStartTime,
                        sceneDuration,
                        visualSettings,
                        defaultEffect,
                        imageItem?.BeatRole ?? "primary",
                        1,
                        1,
                        imageItem?.TransitionType,
                        overlayText: imageItem?.OverlayText,
                        shotType: imageItem?.ShotType,
                        directorIntent: imageItem?.DirectorIntent,
                        continuityAnchor: imageItem?.ContinuityAnchor,
                        composition: imageItem?.Composition,
                        visualType: imageItem?.VisualType,
                        varietyRole: imageItem?.VarietyRole,
                        varietyReason: imageItem?.VarietyReason,
                        visualQualityScore: imageItem?.VisualQualityScore,
                        visualQualityNotes: imageItem?.VisualQualityNotes,
                        sourceImageSceneNumber: GetSourceImageSceneNumber(sceneNumber, imageItem),
                        sourceImageBeatIndex: imageItem?.BeatIndex,
                        isFallbackImage: IsFallbackImage(sceneNumber, imageItem))
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
                    segmentCount,
                    imageItem?.TransitionType,
                    overlayText: segmentIndex == 0 ? imageItem?.OverlayText : "",
                    shotType: imageItem?.ShotType,
                    directorIntent: imageItem?.DirectorIntent,
                    continuityAnchor: imageItem?.ContinuityAnchor,
                    composition: imageItem?.Composition,
                    visualType: imageItem?.VisualType,
                    varietyRole: imageItem?.VarietyRole,
                    varietyReason: imageItem?.VarietyReason,
                    visualQualityScore: imageItem?.VisualQualityScore,
                    visualQualityNotes: imageItem?.VisualQualityNotes,
                    sourceImageSceneNumber: GetSourceImageSceneNumber(sceneNumber, imageItem),
                    sourceImageBeatIndex: imageItem?.BeatIndex,
                    isFallbackImage: IsFallbackImage(sceneNumber, imageItem)));
            }

            return events;
        }

        private static List<VisualEvent> BuildStoryboardVisualEventsForScene(
            int sceneNumber,
            List<SceneImageItem> sceneImages,
            double sceneStartTime,
            double sceneDuration,
            RenderVisualEffectsSettings visualSettings)
        {
            var cleanImages = sceneImages
                .Where(x => !string.IsNullOrWhiteSpace(x.ImagePath))
                .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                .ToList();

            if (cleanImages.Count == 0)
                return new List<VisualEvent>();

            var segmentCount = cleanImages.Count;
            var segmentDuration = sceneDuration / segmentCount;
            var events = new List<VisualEvent>(segmentCount);

            for (var i = 0; i < cleanImages.Count; i++)
            {
                var image = cleanImages[i];
                var start = sceneStartTime + (segmentDuration * i);
                var duration = i == cleanImages.Count - 1
                    ? sceneDuration - (segmentDuration * i)
                    : segmentDuration;

                events.Add(CreateVisualEvent(
                    sceneNumber,
                    image.ImagePath,
                    start,
                    Math.Max(0.25, duration),
                    visualSettings,
                    NormalizeEffectType(image.EffectType) ?? PickEffect(i),
                    string.IsNullOrWhiteSpace(image.BeatRole) ? "storyboard" : image.BeatRole,
                    i + 1,
                    segmentCount,
                    image.TransitionType,
                    overlayText: image.OverlayText,
                    shotType: image.ShotType,
                    directorIntent: image.DirectorIntent,
                    continuityAnchor: image.ContinuityAnchor,
                    composition: image.Composition,
                    visualType: image.VisualType,
                    varietyRole: image.VarietyRole,
                    varietyReason: image.VarietyReason,
                    visualQualityScore: image.VisualQualityScore,
                    visualQualityNotes: image.VisualQualityNotes,
                    sourceImageSceneNumber: GetSourceImageSceneNumber(sceneNumber, image),
                    sourceImageBeatIndex: image.BeatIndex,
                    isFallbackImage: IsFallbackImage(sceneNumber, image)));
            }

            return events;
        }

        private static List<VisualEvent> BuildEditPlanVisualEventsForScene(
            int sceneNumber,
            List<SceneImageItem> sceneImages,
            EditScenePlan editScene,
            double sceneStartTime,
            double sceneDuration,
            RenderVisualEffectsSettings visualSettings)
        {
            var orderedImages = sceneImages
                .Where(x => !string.IsNullOrWhiteSpace(x.ImagePath))
                .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                .ToList();

            var beats = editScene.VisualBeats
                .Where(x => !string.IsNullOrWhiteSpace(x.ImagePath) || orderedImages.Count > 0)
                .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                .Take(12)
                .ToList();

            if (beats.Count == 0)
                return new List<VisualEvent>();

            var totalWeight = beats.Sum(x => Math.Clamp(x.DurationWeight <= 0 ? 1.0 : x.DurationWeight, 0.25, 4.0));
            if (totalWeight <= 0) totalWeight = beats.Count;

            var elapsed = 0.0;
            var events = new List<VisualEvent>(beats.Count);

            for (var i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                var image = FindSceneImage(orderedImages, beat.SourceImageBeatIndex, i + 1);
                var imagePath = !string.IsNullOrWhiteSpace(beat.ImagePath)
                    ? beat.ImagePath
                    : image?.ImagePath ?? "";

                if (string.IsNullOrWhiteSpace(imagePath))
                    continue;

                var weight = Math.Clamp(beat.DurationWeight <= 0 ? 1.0 : beat.DurationWeight, 0.25, 4.0);
                var duration = i == beats.Count - 1
                    ? sceneDuration - elapsed
                    : sceneDuration * (weight / totalWeight);

                if (duration <= 0.05)
                    continue;

                var clampedDuration = Math.Max(0.25, duration);
                if (elapsed + clampedDuration > sceneDuration)
                    clampedDuration = Math.Max(0.05, sceneDuration - elapsed);

                if (clampedDuration <= 0.05)
                    continue;

                var transitionDuration = Math.Min(
                    Math.Clamp(beat.TransitionDuration, 0.0, 1.5),
                    Math.Max(0, clampedDuration / 2.0));

                events.Add(CreateVisualEvent(
                    sceneNumber,
                    imagePath,
                    sceneStartTime + elapsed,
                    clampedDuration,
                    visualSettings,
                    NormalizeEffectType(beat.EffectType) ?? NormalizeEffectType(image?.EffectType) ?? PickEffect(i),
                    string.IsNullOrWhiteSpace(beat.VisualRole) ? image?.BeatRole ?? "edit_plan" : beat.VisualRole,
                    i + 1,
                    beats.Count,
                    string.IsNullOrWhiteSpace(beat.TransitionType) ? image?.TransitionType : beat.TransitionType,
                    transitionDuration,
                    beat.OverlayText,
                    beat.Emphasis,
                    string.IsNullOrWhiteSpace(beat.ShotType) ? image?.ShotType : beat.ShotType,
                    string.IsNullOrWhiteSpace(beat.DirectorIntent) ? image?.DirectorIntent : beat.DirectorIntent,
                    editScene.ChapterTitle,
                    editScene.CaptionMode,
                    editScene.MusicEnergy,
                    string.IsNullOrWhiteSpace(editScene.ContinuityAnchor) ? image?.ContinuityAnchor : editScene.ContinuityAnchor,
                    string.IsNullOrWhiteSpace(beat.Composition) ? image?.Composition : beat.Composition,
                    image?.VisualType,
                    image?.VarietyRole,
                    image?.VarietyReason,
                    image?.VisualQualityScore,
                    image?.VisualQualityNotes,
                    beat.SegmentRole,
                    beat.CutReason,
                    sourceImageSceneNumber: GetSourceImageSceneNumber(sceneNumber, image),
                    sourceImageBeatIndex: image?.BeatIndex ?? beat.SourceImageBeatIndex,
                    isFallbackImage: IsFallbackImage(sceneNumber, image)));

                elapsed += clampedDuration;
                if (elapsed >= sceneDuration) break;
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
            int segmentCount,
            string? transitionType = null,
            double? transitionDuration = null,
            string? overlayText = null,
            string? emphasis = null,
            string? shotType = null,
            string? directorIntent = null,
            string? chapterTitle = null,
            string? captionMode = null,
            string? musicEnergy = null,
            string? continuityAnchor = null,
            string? composition = null,
            string? visualType = null,
            string? varietyRole = null,
            string? varietyReason = null,
            int? visualQualityScore = null,
            string? visualQualityNotes = null,
            string? segmentRole = null,
            string? cutReason = null,
            int? sourceImageSceneNumber = null,
            int? sourceImageBeatIndex = null,
            bool isFallbackImage = false)
        {
            return new VisualEvent
            {
                SceneIndex = sceneNumber,
                ImagePath = imagePath,
                StartTime = startTime,
                Duration = duration,
                EffectType = effectType,
                ZoomIntensity = visualSettings.ZoomIntensity,
                TransitionType = string.IsNullOrWhiteSpace(transitionType) ? visualSettings.TransitionType : transitionType,
                TransitionDuration = transitionDuration ?? visualSettings.TransitionDurationSec,
                OverlayText = overlayText ?? "",
                Emphasis = emphasis ?? "",
                VisualRole = visualRole,
                VisualType = visualType ?? "",
                VarietyRole = varietyRole ?? "",
                VarietyReason = varietyReason ?? "",
                SegmentRole = string.IsNullOrWhiteSpace(segmentRole) ? InferSegmentRole(visualRole, segmentIndex, segmentCount) : segmentRole,
                SegmentIndex = segmentIndex,
                SegmentCount = segmentCount,
                ShotType = shotType ?? "",
                DirectorIntent = directorIntent ?? "",
                CutReason = cutReason ?? "",
                ChapterTitle = chapterTitle ?? "",
                CaptionMode = captionMode ?? "",
                MusicEnergy = musicEnergy ?? "",
                ContinuityAnchor = continuityAnchor ?? "",
                Composition = composition ?? "",
                VisualQualityScore = visualQualityScore ?? 0,
                VisualQualityNotes = visualQualityNotes ?? "",
                SourceImageSceneNumber = sourceImageSceneNumber ?? sceneNumber,
                SourceImageBeatIndex = sourceImageBeatIndex ?? segmentIndex,
                IsFallbackImage = isFallbackImage
            };
        }

        private static List<EditDecisionItem> BuildEditDecisionList(List<VisualEvent> visualTrack, List<AudioEvent> audioTrack)
        {
            var voiceByScene = audioTrack
                .Where(x => string.Equals(x.Type, "voice", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.SceneNumber)
                .ToDictionary(x => x.Key, x => x.OrderBy(a => a.StartTime).First());

            return visualTrack
                .OrderBy(x => x.StartTime)
                .Select((visual, index) =>
                {
                    voiceByScene.TryGetValue(visual.SceneIndex, out var audio);

                    return new EditDecisionItem
                    {
                        Index = index + 1,
                        SceneNumber = visual.SceneIndex,
                        StartTime = visual.StartTime,
                        EndTime = visual.StartTime + visual.Duration,
                        Duration = visual.Duration,
                        SegmentRole = visual.SegmentRole,
                        VisualRole = visual.VisualRole,
                        VisualType = visual.VisualType,
                        TransitionType = visual.TransitionType,
                        EffectType = visual.EffectType,
                        CutReason = visual.CutReason,
                        DirectorIntent = visual.DirectorIntent,
                        ChapterTitle = visual.ChapterTitle,
                        OverlayText = visual.OverlayText,
                        MusicEnergy = visual.MusicEnergy,
                        CaptionMode = visual.CaptionMode,
                        AudioTransition = audio?.EditTransition ?? "",
                        AudioOffsetSec = audio?.EditOffsetSec ?? 0,
                        ImagePath = visual.ImagePath,
                        SourceImageSceneNumber = visual.SourceImageSceneNumber,
                        SourceImageBeatIndex = visual.SourceImageBeatIndex,
                        IsFallbackImage = visual.IsFallbackImage
                    };
                })
                .ToList();
        }

        private static List<BrollLayerItem> BuildBrollLayerPlan(List<VisualEvent> visualTrack)
        {
            return visualTrack
                .OrderBy(x => x.StartTime)
                .Where(IsBrollLayerCandidate)
                .Select(visual => new BrollLayerItem
                {
                    SceneNumber = visual.SceneIndex,
                    SegmentIndex = visual.SegmentIndex,
                    LayerType = ResolveLayerType(visual),
                    VisualType = visual.VisualType,
                    VisualRole = visual.VisualRole,
                    StartTime = visual.StartTime,
                    EndTime = visual.StartTime + visual.Duration,
                    Duration = visual.Duration,
                    ImagePath = visual.ImagePath,
                    Reason = !string.IsNullOrWhiteSpace(visual.VarietyReason)
                        ? visual.VarietyReason
                        : !string.IsNullOrWhiteSpace(visual.CutReason)
                            ? visual.CutReason
                            : visual.DirectorIntent
                })
                .ToList();
        }

        private static bool IsBrollLayerCandidate(VisualEvent visual)
            => IsInfoVisualType(visual.VisualType)
               || visual.VisualRole.Contains("broll", StringComparison.OrdinalIgnoreCase)
               || visual.SegmentRole.Contains("detail", StringComparison.OrdinalIgnoreCase)
               || visual.SegmentRole.Contains("emphasis", StringComparison.OrdinalIgnoreCase);

        private static string ResolveLayerType(VisualEvent visual)
        {
            var visualType = NormalizeEditToken(visual.VisualType);
            if (visualType is "map" or "timeline" or "diagram")
                return "info_visual";
            if (visualType == "comparison")
                return "contrast_visual";
            if (visual.VisualRole.Contains("broll", StringComparison.OrdinalIgnoreCase))
                return "broll_motion";
            return "editor_detail";
        }

        private static bool IsInfoVisualType(string? visualType)
        {
            var token = NormalizeEditToken(visualType);
            return token is "map" or "timeline" or "diagram" or "comparison";
        }

        private static SceneLayoutReviewReport BuildReviewReport(SceneLayoutStagePayload layout, int expectedSceneCount)
        {
            var report = new SceneLayoutReviewReport
            {
                SceneCount = expectedSceneCount,
                VisualCount = layout.VisualTrack.Count,
                AudioCount = layout.AudioTrack.Count,
                CaptionCount = layout.CaptionTrack.Count
            };

            if (layout.VisualTrack.Count == 0)
            {
                AddReviewIssue(report, "Error", "timeline.no_visuals", "Timeline içinde hiç görsel yok.", "SceneLayout girdilerini ve Image stage çıktısını kontrol et.");
            }

            var visualScenes = layout.VisualTrack
                .Select(x => x.SceneIndex)
                .Where(x => x > 0)
                .Distinct()
                .ToHashSet();

            foreach (var sceneNumber in Enumerable.Range(1, Math.Max(0, expectedSceneCount)).Where(x => !visualScenes.Contains(x)).Take(20))
            {
                AddReviewIssue(report, "Error", "timeline.missing_scene_visual", $"Sahne {sceneNumber} timeline içinde görsel alamadı.", "Bu sahne için görseli yeniden üret veya SceneLayout aşamasını tekrar çalıştır.", sceneNumber);
            }

            foreach (var visual in layout.VisualTrack)
            {
                if (string.IsNullOrWhiteSpace(visual.ImagePath))
                {
                    report.MissingImageCount++;
                    AddReviewIssue(report, "Error", "visual.missing_image", $"Sahne {visual.SceneIndex} için image path boş.", "Görseli yeniden üret.", visual.SceneIndex, visual.SegmentIndex);
                }

                if (visual.IsFallbackImage)
                {
                    report.FallbackImageCount++;
                    var source = visual.SourceImageSceneNumber > 0 ? $"S{visual.SourceImageSceneNumber}" : "önceki sahne";
                    AddReviewIssue(report, "Warning", "visual.fallback_image", $"Sahne {visual.SceneIndex} kendi görseli yerine {source} görselini kullanıyor.", "Bu karttan görseli yeniden üret.", visual.SceneIndex, visual.SegmentIndex, visual.ImagePath);
                }

                if (visual.VisualQualityScore is > 0 and < 62)
                {
                    report.LowQualityImageCount++;
                    AddReviewIssue(report, "Warning", "visual.low_quality_score", $"Sahne {visual.SceneIndex} görsel QA skoru düşük: {visual.VisualQualityScore}/100.", "Promptu veya görseli kontrol edip regenerate et.", visual.SceneIndex, visual.SegmentIndex, visual.ImagePath);
                }

                if (visual.Duration is > 0 and < 0.75)
                {
                    AddReviewIssue(report, "Warning", "timeline.short_visual_segment", $"Sahne {visual.SceneIndex} içinde çok kısa görsel segment var: {visual.Duration:F2} sn.", "EditPlan duration weight değerlerini kontrol et.", visual.SceneIndex, visual.SegmentIndex, visual.ImagePath);
                }
            }

            var duplicateGroups = layout.VisualTrack
                .Where(x => !string.IsNullOrWhiteSpace(x.ImagePath))
                .GroupBy(x => NormalizeImageFileName(x.ImagePath), StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Select(v => v.SceneIndex).Distinct().Count() > 1)
                .ToList();

            foreach (var group in duplicateGroups.Take(20))
            {
                var scenes = string.Join(", ", group.Select(x => x.SceneIndex).Distinct().OrderBy(x => x).Take(8).Select(x => $"S{x}"));
                report.DuplicateImageCount += group.Count();
                AddReviewIssue(report, "Warning", "visual.duplicate_image", $"Aynı görsel birden fazla sahnede kullanılıyor: {scenes}.", "Bilinçli tekrar değilse ilgili sahneleri regenerate et.", group.First().SceneIndex, group.First().SegmentIndex, group.First().ImagePath);
            }

            var qualityScores = layout.VisualTrack
                .Select(x => x.VisualQualityScore)
                .Where(x => x > 0)
                .ToList();
            report.AverageVisualQualityScore = qualityScores.Count == 0 ? 0 : Math.Round(qualityScores.Average(), 1);

            if (layout.CaptionTrack.Count == 0)
            {
                AddReviewIssue(report, "Info", "caption.no_word_timing", "Caption/STT verisi yok veya timeline'a taşınmadı.", "Kelime zamanlamalı altyazı istiyorsan STT stage çıktısını kontrol et.");
            }

            report.IssueCount = report.Issues.Count;
            report.ErrorCount = report.Issues.Count(x => x.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
            report.WarningCount = report.Issues.Count(x => x.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
            report.InfoCount = report.Issues.Count(x => x.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase));
            report.Status = report.ErrorCount > 0 ? "Blocked" : report.WarningCount > 0 ? "Review" : "Ready";

            return report;
        }

        private static void AddReviewIssue(
            SceneLayoutReviewReport report,
            string severity,
            string code,
            string message,
            string actionHint,
            int? sceneNumber = null,
            int? segmentIndex = null,
            string? imagePath = null)
        {
            report.Issues.Add(new SceneLayoutReviewIssue
            {
                Severity = severity,
                Code = code,
                Message = message,
                ActionHint = actionHint,
                SceneNumber = sceneNumber,
                SegmentIndex = segmentIndex,
                ImagePath = imagePath ?? ""
            });
        }

        private static string NormalizeImageFileName(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return "";

            try
            {
                return Path.GetFileName(imagePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
            }
            catch
            {
                return "";
            }
        }

        private static string FormatReviewStatus(string status)
            => status switch
            {
                "Blocked" => "blokaj var",
                "Review" => "kontrol önerilir",
                _ => "hazır"
            };

        private static AudioEditDecision ResolveAudioEditDecision(
            EditScenePlan? editScene,
            List<VisualEvent> visualEvents,
            int sceneIndexZeroBased,
            double sceneDuration,
            RenderAudioMixSettings audioSettings)
        {
            var maxOffset = Math.Clamp(audioSettings.MaxEditorAudioOffsetSec, 0.0, 0.40);
            if (!audioSettings.EnableEditorAudioCuts || editScene == null || sceneIndexZeroBased <= 0 || sceneDuration < 2.4 || maxOffset <= 0)
                return AudioEditDecision.Straight(audioSettings);

            var firstVisual = visualEvents
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();
            var firstBeat = editScene.VisualBeats
                .OrderBy(x => x.BeatIndex)
                .FirstOrDefault();

            var intent = NormalizeEditToken(editScene.Intent);
            var pacing = NormalizeEditToken(editScene.Pacing);
            var segmentRole = NormalizeEditToken(firstVisual?.SegmentRole ?? firstBeat?.SegmentRole);
            var transition = NormalizeEditToken(firstVisual?.TransitionType ?? firstBeat?.TransitionType);
            var fade = Math.Clamp(audioSettings.VoiceMicroFadeSec, 0.0, 0.12);

            if (transition is "flash" or "flash_white" or "dip_black" or "dip_to_black")
                return AudioEditDecision.Straight(audioSettings);

            var offset = Math.Min(maxOffset, Math.Max(0.10, sceneDuration * 0.045));

            if (segmentRole is "emphasis" or "payoff" || intent is "hook" or "reveal" or "proof")
                return new AudioEditDecision("j_cut", -offset, fade, 0);

            if (segmentRole is "transition" || intent is "context" or "setup" or "recap")
                return new AudioEditDecision("l_cut", Math.Min(offset, 0.18), fade, 0);

            if (pacing is "fast" or "urgent" && sceneIndexZeroBased % 4 == 0)
                return new AudioEditDecision("j_cut", -Math.Min(offset, 0.16), fade, 0);

            return AudioEditDecision.Straight(audioSettings);
        }

        private static string NormalizeEditToken(string? value)
            => (value ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

        private static string FormatAudioEditName(string type)
            => type switch
            {
                "j_cut" => "J-cut",
                "l_cut" => "L-cut",
                _ => "Düz"
            };

        private readonly record struct AudioEditDecision(string Type, double OffsetSec, double FadeInSec, double FadeOutSec)
        {
            public static AudioEditDecision Straight(RenderAudioMixSettings settings)
            {
                var fade = Math.Clamp(settings.VoiceMicroFadeSec, 0.0, 0.08);
                return new AudioEditDecision("straight", 0, fade, 0);
            }
        }

        private static string InferSegmentRole(string? visualRole, int segmentIndex, int segmentCount)
        {
            var role = (visualRole ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

            if (segmentIndex <= 1)
                return "establishing";
            if (role.Contains("detail") || role.Contains("broll"))
                return "detail";
            if (role.Contains("reveal") || role.Contains("payoff"))
                return "payoff";
            if (role.Contains("chapter") || role.Contains("transition"))
                return "transition";
            if (segmentIndex == segmentCount)
                return "emphasis";

            return "detail";
        }

        private static bool IsFallbackImage(int sceneNumber, SceneImageItem? image)
        {
            if (image == null) return false;
            if (image.SceneNumber != sceneNumber) return true;

            var role = (image.BeatRole ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            return role.Contains("fallback");
        }

        private static int? GetSourceImageSceneNumber(int sceneNumber, SceneImageItem? image)
        {
            if (image == null) return null;
            if (!IsFallbackImage(sceneNumber, image)) return image.SceneNumber;

            return TryParseSceneNumberFromImagePath(image.ImagePath) ?? image.SceneNumber;
        }

        private static int? TryParseSceneNumberFromImagePath(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;

            var fileName = Path.GetFileName(imagePath);
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            const string prefix = "scene_";
            var index = fileName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            var start = index + prefix.Length;
            var digits = new string(fileName.Skip(start).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var scene) ? scene : null;
        }

        private static async Task<int> AddAudioCuesAsync(
            SceneLayoutStagePayload layout,
            EditScenePlan editScene,
            double sceneStartTime,
            double sceneDuration,
            RenderAudioMixSettings audioSettings,
            Func<string, Task> logAsync)
        {
            var added = 0;

            foreach (var cue in editScene.AudioCues)
            {
                var cueType = NormalizeCueType(cue.Type);
                if (string.IsNullOrWhiteSpace(cueType) || cueType is "none" or "silence")
                    continue;

                var sfxPath = ResolveSfxCuePath(cueType);
                if (string.IsNullOrWhiteSpace(sfxPath))
                {
                    sfxPath = $"synth://{cueType}";
                    await logAsync(PipelineLiveLog.Info($"Audio cue '{cue.Type}' için SFX dosyası bulunamadı. Sentetik cue kullanılacak."));
                }

                layout.AudioTrack.Add(new AudioEvent
                {
                    Type = "sfx",
                    FilePath = sfxPath,
                    StartTime = sceneStartTime + Math.Clamp(cue.AtSec, 0, Math.Max(0, sceneDuration - 0.05)),
                    Duration = 0.5,
                    Volume = Math.Clamp(audioSettings.SfxVolumePercent / 100.0, 0.0, 2.0),
                    Loop = false
                });
                added++;
            }

            return added;
        }

        private static string NormalizeCueType(string? value)
        {
            var token = (value ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

            return token switch
            {
                "hit" or "impact" or "stab" => "hit",
                "whoosh" or "swoosh" or "swish" => "whoosh",
                "low_boom" or "boom" or "sub_boom" or "cinematic_boom" => "low_boom",
                "silence" or "pause" => "silence",
                "none" or "no" or "off" or "sfx" or "sound_effect" or "sound_fx" or "effect" or "music" => "none",
                _ => token
            };
        }

        private static string? ResolveSfxCuePath(string cueType)
        {
            var assetRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets");
            var folders = new[]
            {
                Path.Combine(assetRoot, "sfx"),
                Path.Combine(assetRoot, "Sfx"),
                Path.Combine(assetRoot, "audio"),
                Path.Combine(assetRoot, "music")
            };

            var candidates = cueType switch
            {
                "hit" or "impact" => new[] { "hit", "impact", "stab" },
                "whoosh" or "swoosh" => new[] { "whoosh", "swoosh", "swish" },
                "low_boom" or "boom" => new[] { "low_boom", "boom", "sub_boom", "cinematic_boom" },
                _ => new[] { cueType }
            };

            foreach (var folder in folders.Where(Directory.Exists))
            {
                var files = Directory
                    .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(x => x.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                                || x.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                                || x.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase)
                                || x.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var candidate in candidates)
                {
                    var match = files.FirstOrDefault(file =>
                        Path.GetFileNameWithoutExtension(file)
                            .Contains(candidate, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(match))
                        return match;
                }
            }

            return null;
        }

        private static SceneImageItem? FindSceneImage(List<SceneImageItem> sceneImages, int sourceBeatIndex, int fallbackIndex)
        {
            if (sceneImages.Count == 0) return null;

            return sceneImages.FirstOrDefault(x => x.BeatIndex == sourceBeatIndex)
                ?? sceneImages.ElementAtOrDefault(Math.Clamp(fallbackIndex - 1, 0, sceneImages.Count - 1))
                ?? sceneImages.FirstOrDefault();
        }

        private static string? NormalizeEffectType(string? value)
        {
            var token = (value ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            return token switch
            {
                "slow_push_in" or "push_in" or "zoom_in" => "zoom_in",
                "slow_pull_out" or "pull_out" or "zoom_out" => "zoom_out",
                "pan_left" => "pan_left",
                "pan_right" => "pan_right",
                "pan_up" => "pan_up",
                "pan_down" => "pan_down",
                "static" or "static_hold" => "static",
                _ => null
            };
        }

        private static string PickEffect(int index)
        {
            var effects = new[] { "zoom_in", "pan_left", "zoom_out", "pan_right", "pan_up", "pan_down" };
            return effects[Math.Abs(index) % effects.Length];
        }
    }
}
