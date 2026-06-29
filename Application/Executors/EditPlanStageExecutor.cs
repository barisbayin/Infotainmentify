using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using NAudio.Wave;
using System.Text.Json;

namespace Application.Executors
{
    [StageExecutor(StageType.EditPlan)]
    public class EditPlanStageExecutor : BaseStageExecutor
    {
        private const int MaxVisualBeatsPerScene = 5;

        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IRepository<ScriptPreset> _scriptPresetRepo;

        public EditPlanStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IRepository<ScriptPreset> scriptPresetRepo)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _scriptPresetRepo = scriptPresetRepo;
        }

        public override StageType StageType => StageType.EditPlan;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            var imageData = context.GetOutput<ImageStagePayload>(StageType.Image);
            var ttsData = context.GetOutput<TtsStagePayload>(StageType.Tts);

            if (scriptData.Scenes.Count == 0)
                throw new InvalidOperationException("Kurgu motoru için Script sahneleri bulunamadı.");

            StoryboardStagePayload? storyboard = null;
            if (context.HasOutput(StageType.Storyboard))
                storyboard = context.GetOutput<StoryboardStagePayload>(StageType.Storyboard);

            SttStagePayload? sttData = null;
            if (context.HasOutput(StageType.Stt))
                sttData = context.GetOutput<SttStagePayload>(StageType.Stt);

            var audioDurations = BuildAudioDurationMap(scriptData, ttsData);
            var imageCount = imageData.SceneImages.Count;
            var wordCount = sttData?.Subtitles.Count ?? 0;

            await logAsync($"EditPlan hazırlanıyor. Sahne: {scriptData.Scenes.Count}, görsel beat: {imageCount}, kelime zamanı: {wordCount}.");

            var scriptPreset = await TryResolveScriptPresetAsync(run, ct);
            if (scriptPreset == null)
            {
                await logAsync(PipelineLiveLog.Warning("Script preset bulunamadı. EditPlan deterministik kurgu motoru ile üretilecek."));
                return BuildFallbackPlan(scriptData, imageData, storyboard, sttData, audioDurations);
            }

            try
            {
                var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, scriptPreset.UserAiConnectionId, ct);
                var prompt = BuildEditPlanPrompt(scriptData, imageData, storyboard, sttData, audioDurations, scriptPreset);

                await logAsync($"AI edit karar isteği gönderiliyor. Model: {scriptPreset.ModelName}.");
                var response = await aiClient.GenerateTextAsync(
                    prompt,
                    temperature: 0.68,
                    model: scriptPreset.ModelName,
                    ct: ct);

                var plan = ParseEditPlan(response, scriptData, imageData, storyboard, sttData, audioDurations);
                await logAsync(PipelineLiveLog.Success($"EditPlan üretildi. Kurgu vuruşu: {plan.Scenes.Sum(x => x.VisualBeats.Count)}."));
                return plan;
            }
            catch (Exception ex)
            {
                await logAsync(PipelineLiveLog.Warning($"AI edit planı üretilemedi. Fallback kurgu planı kullanılacak. Hata: {PipelineLiveLog.Shorten(ex.Message, 280)}"));
                return BuildFallbackPlan(scriptData, imageData, storyboard, sttData, audioDurations);
            }
        }

        private async Task<ScriptPreset?> TryResolveScriptPresetAsync(ContentPipelineRun run, CancellationToken ct)
        {
            var scriptPresetId = run.Template?.StageConfigs?
                .OrderBy(x => x.Order)
                .FirstOrDefault(x => x.StageType == StageType.Script)
                ?.PresetId;

            if (!scriptPresetId.HasValue || scriptPresetId.Value <= 0) return null;
            return await _scriptPresetRepo.GetByIdAsync(scriptPresetId.Value, true, ct);
        }

        private static Dictionary<int, double> BuildAudioDurationMap(ScriptStagePayload scriptData, TtsStagePayload ttsData)
        {
            var map = new Dictionary<int, double>();

            foreach (var scene in scriptData.Scenes)
            {
                var audio = ttsData.SceneAudios.FirstOrDefault(x => x.SceneNumber == scene.SceneNumber);
                var duration = scene.EstimatedDuration > 0 ? scene.EstimatedDuration : 5.0;

                if (audio != null && File.Exists(audio.AudioFilePath))
                {
                    try
                    {
                        using var reader = new AudioFileReader(audio.AudioFilePath);
                        duration = Math.Max(0.25, reader.TotalTime.TotalSeconds);
                    }
                    catch
                    {
                        duration = Math.Max(0.25, duration);
                    }
                }

                map[scene.SceneNumber] = Math.Max(0.25, duration);
            }

            return map;
        }

        private static string BuildEditPlanPrompt(
            ScriptStagePayload script,
            ImageStagePayload imageData,
            StoryboardStagePayload? storyboard,
            SttStagePayload? sttData,
            IReadOnlyDictionary<int, double> audioDurations,
            ScriptPreset scriptPreset)
        {
            var scenes = script.Scenes
                .OrderBy(x => x.SceneNumber)
                .Select(scene =>
                {
                    var storyScene = storyboard?.Scenes.FirstOrDefault(x => x.SceneNumber == scene.SceneNumber);
                    var images = imageData.SceneImages
                        .Where(x => x.SceneNumber == scene.SceneNumber)
                        .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                        .Select(x => new
                        {
                            imageBeatIndex = x.BeatIndex <= 0 ? 1 : x.BeatIndex,
                            role = x.BeatRole,
                            visualType = x.VisualType,
                            varietyRole = x.VarietyRole,
                            varietyReason = x.VarietyReason,
                            shot = x.ShotType,
                            motion = x.EffectType,
                            transition = x.TransitionType,
                            overlayText = x.OverlayText,
                            directorIntent = x.DirectorIntent,
                            continuityAnchor = x.ContinuityAnchor,
                            composition = x.Composition,
                            lens = x.Lens,
                            lighting = x.Lighting,
                            colorNotes = x.ColorNotes,
                            cutIntent = x.CutIntent,
                            visualQualityScore = x.VisualQualityScore,
                            visualQualityNotes = x.VisualQualityNotes
                        })
                        .ToList();

                    return new
                    {
                        sceneNumber = scene.SceneNumber,
                        audioDurationSec = audioDurations.TryGetValue(scene.SceneNumber, out var duration) ? duration : scene.EstimatedDuration,
                        estimatedDuration = scene.EstimatedDuration,
                        narration = ShortenForPrompt(scene.AudioText, 1100),
                        chapterIndex = storyScene?.ChapterIndex,
                        chapterTitle = storyScene?.ChapterTitle,
                        sceneType = storyScene?.SceneType,
                        scenePurpose = storyScene?.ScenePurpose,
                        retentionGoal = storyScene?.RetentionGoal,
                        emotionalTone = storyScene?.EmotionalTone,
                        visualContrast = storyScene?.VisualContrast,
                        continuityAnchor = storyScene?.ContinuityAnchor,
                        musicEnergy = storyScene?.MusicEnergy,
                        captionMode = storyScene?.CaptionMode,
                        storyboardTransition = storyScene?.TransitionType,
                        storyboardOverlay = storyScene?.OverlayText,
                        availableImages = images,
                        transcriptDigest = BuildTranscriptDigest(sttData, scene.SceneNumber)
                    };
                });

            var sceneJson = JsonSerializer.Serialize(scenes);

            return $$"""
You are a senior human video editor creating an edit decision plan for a long-form YouTube video.
You do not render. You decide pacing, cuts, visual beat timing, motion and transition choices.

Video title: {{script.Title}}
Language/culture: {{scriptPreset.Language}}
Tone: {{scriptPreset.Tone}}
Storyboard style bible: {{storyboard?.StyleBible ?? "not provided"}}
Director continuity bible: {{storyboard?.VisualContinuityBible ?? "not provided"}}
Camera language: {{storyboard?.CameraLanguage ?? "not provided"}}
Editing principles: {{storyboard?.EditingPrinciples ?? "not provided"}}
Chapter strategy: {{storyboard?.ChapterStrategy ?? "not provided"}}

Return ONLY raw JSON. No markdown, no comments.

Rules:
- Keep the same sceneNumber values.
- Use only imageBeatIndex values listed in availableImages for each scene.
- Do not invent file paths.
- For each scene create 1 to {{MaxVisualBeatsPerScene}} visualBeats.
- More important or longer scenes may use more beats; very short scenes should stay simple.
- If a scene is around 8 seconds or longer, use at least 2 visualBeats when availableImages has at least 2 items. If it is around 16 seconds or longer, use at least 3 when available.
- Vary motion and transition tastefully. Avoid making every beat zoom_in.
- Prefer clean editorial cuts; use flash/dip_black only for real emphasis or chapter breaks.
- Think in mini segments: establishing starts the idea, detail gives evidence, emphasis lands the point, transition bridges chapters, payoff resolves a reveal.
- Do not cut on every sentence. Cut on meaning changes, proof/examples, reveals, contrast, or intentional visual refresh.
- durationWeight is relative, not seconds. The timeline engine will map weights onto exact audio duration.
- effectType must be one of: zoom_in, zoom_out, pan_left, pan_right, pan_up, pan_down, static.
- transitionType must be one of: cut, crossfade, dip_black, flash, match_cut.
- audioCues.type must be exactly one of: hit, whoosh, low_boom, silence, none.
- Do not use generic values like sfx, sound_effect, effect, music, boom_sound.
- Use audio cues sparingly; choose none unless the cue has a clear editorial purpose.
- overlayText must be short or empty. Do not ask the image itself to contain text.

JSON shape:
{
  "pacingProfile": "documentary | fast_hook | calm_explainer | investigative | cinematic",
  "transitionPalette": "editorial_cuts | soft_cinematic | punchy_retention | chaptered",
  "captionStrategy": "word_sync | keyword_emphasis | calm_subtitles",
  "audioMood": "short music/sfx direction",
  "directorSummary": "one paragraph edit philosophy for this video",
  "visualContinuityNotes": "how to preserve continuity through cuts",
  "musicEnergyCurve": "how energy changes across chapters",
  "silenceStrategy": "where silence/space should be used",
  "scenes": [
    {
      "sceneNumber": 1,
      "intent": "hook | context | explanation | proof | example | reveal | recap | outro",
      "pacing": "fast | balanced | slow | impact",
      "chapterTitle": "short chapter title",
      "retentionGoal": "why viewer keeps watching",
      "musicEnergy": "low | medium | high | silence",
      "captionMode": "word_sync | keyword_emphasis | calm_subtitles",
      "continuityAnchor": "visual motif",
      "editorNote": "why this scene is cut this way",
      "visualBeats": [
        {
          "beatIndex": 1,
          "imageBeatIndex": 1,
          "visualRole": "primary | detail | broll | contrast | reveal | chapter_card",
          "segmentRole": "establishing | detail | emphasis | transition | payoff",
          "effectType": "zoom_in",
          "transitionType": "cut",
          "transitionDuration": 0.35,
          "durationWeight": 1.0,
          "overlayText": "",
          "cutReason": "why this cut exists",
          "emphasis": "keywords or emotion to emphasize",
          "shotType": "wide shot | close-up | macro detail | top-down",
          "composition": "framing note",
          "continuityNotes": "how this cut preserves continuity",
          "directorIntent": "human-readable reason for the beat"
        }
      ],
      "captionCues": [
        { "text": "short phrase", "startSec": 0.0, "endSec": 0.0, "emphasis": "optional" }
      ],
      "audioCues": [
        { "type": "hit | whoosh | low_boom | silence | none", "atSec": 0.0, "note": "optional" }
      ]
    }
  ]
}

Scene inputs:
{{sceneJson}}
""";
        }

        private static EditPlanStagePayload ParseEditPlan(
            string response,
            ScriptStagePayload scriptData,
            ImageStagePayload imageData,
            StoryboardStagePayload? storyboard,
            SttStagePayload? sttData,
            IReadOnlyDictionary<int, double> audioDurations)
        {
            var cleanJson = CleanJson(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var payload = new EditPlanStagePayload
            {
                ScriptId = scriptData.ScriptId,
                PacingProfile = NormalizeToken(GetStr(root, "pacingProfile", "pacing"), "documentary"),
                TransitionPalette = NormalizeToken(GetStr(root, "transitionPalette", "transitionStyle"), "editorial_cuts"),
                CaptionStrategy = NormalizeToken(GetStr(root, "captionStrategy", "captionStyle"), "word_sync"),
                AudioMood = NormalizeText(GetStr(root, "audioMood", "musicMood", "soundDirection"), ""),
                DirectorSummary = NormalizeText(GetStr(root, "directorSummary", "editPhilosophy"), ""),
                VisualContinuityNotes = NormalizeText(GetStr(root, "visualContinuityNotes", "continuityNotes"), storyboard?.VisualContinuityBible ?? ""),
                MusicEnergyCurve = NormalizeText(GetStr(root, "musicEnergyCurve", "energyCurve"), ""),
                SilenceStrategy = NormalizeText(GetStr(root, "silenceStrategy", "pauseStrategy"), "")
            };

            if (!TryGetProperty(root, out var scenesElement, "scenes", "items") || scenesElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("EditPlan JSON içinde scenes array bulunamadı.");

            foreach (var sceneElement in scenesElement.EnumerateArray())
            {
                if (sceneElement.ValueKind != JsonValueKind.Object) continue;

                var sceneNumber = GetInt(sceneElement, 0, "sceneNumber", "scene", "number");
                var sourceScene = scriptData.Scenes.FirstOrDefault(x => x.SceneNumber == sceneNumber);
                if (sourceScene == null) continue;

                var sceneImages = imageData.SceneImages
                    .Where(x => x.SceneNumber == sceneNumber)
                    .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                    .ToList();
                var storyboardScene = storyboard?.Scenes.FirstOrDefault(x => x.SceneNumber == sceneNumber);

                var plan = new EditScenePlan
                {
                    SceneNumber = sceneNumber,
                    Intent = NormalizeToken(GetStr(sceneElement, "intent", "sceneIntent", "sceneType"), storyboardScene?.SceneType ?? "explanation"),
                    Pacing = NormalizePacing(GetStr(sceneElement, "pacing", "tempo")),
                    AudioDurationSec = audioDurations.TryGetValue(sceneNumber, out var duration) ? duration : Math.Max(0.25, sourceScene.EstimatedDuration),
                    ChapterTitle = NormalizeText(GetStr(sceneElement, "chapterTitle"), storyboardScene?.ChapterTitle ?? ""),
                    RetentionGoal = NormalizeText(GetStr(sceneElement, "retentionGoal", "retention"), storyboardScene?.RetentionGoal ?? ""),
                    MusicEnergy = NormalizeEnergy(GetStr(sceneElement, "musicEnergy", "energy"), storyboardScene?.MusicEnergy),
                    CaptionMode = NormalizeToken(GetStr(sceneElement, "captionMode", "captionStrategy"), storyboardScene?.CaptionMode ?? payload.CaptionStrategy),
                    ContinuityAnchor = NormalizeText(GetStr(sceneElement, "continuityAnchor", "anchor", "motif"), storyboardScene?.ContinuityAnchor ?? ""),
                    EditorNote = NormalizeText(GetStr(sceneElement, "editorNote", "note"), "")
                };

                if (TryGetProperty(sceneElement, out var beatsElement, "visualBeats", "beats", "shots")
                    && beatsElement.ValueKind == JsonValueKind.Array)
                {
                    var index = 1;
                    foreach (var beatElement in beatsElement.EnumerateArray().Take(MaxVisualBeatsPerScene))
                    {
                        if (beatElement.ValueKind != JsonValueKind.Object) continue;

                        var sourceBeatIndex = GetInt(beatElement, index, "imageBeatIndex", "sourceImageBeatIndex", "sourceBeat", "beatIndex");
                        var sourceImage = FindSceneImage(sceneImages, sourceBeatIndex, index);

                        plan.VisualBeats.Add(new EditVisualBeatPlan
                        {
                            BeatIndex = index,
                            SourceImageBeatIndex = sourceImage?.BeatIndex > 0 ? sourceImage.BeatIndex : Math.Max(1, sourceBeatIndex),
                            ImagePath = sourceImage?.ImagePath ?? "",
                            VisualRole = NormalizeToken(GetStr(beatElement, "visualRole", "role"), sourceImage?.BeatRole ?? (index == 1 ? "primary" : "broll")),
                            SegmentRole = NormalizeSegmentRole(GetStr(beatElement, "segmentRole", "miniSegment", "editRole"), index, plan.Intent),
                            EffectType = NormalizeEffect(GetStr(beatElement, "effectType", "motion", "cameraMotion"), sourceImage?.EffectType),
                            TransitionType = NormalizeTransitionForBeat(
                                GetStr(beatElement, "transitionType", "transition"),
                                sourceImage?.TransitionType,
                                index,
                                plan.Intent,
                                GetStr(beatElement, "segmentRole", "miniSegment", "editRole")),
                            TransitionDuration = Math.Clamp(GetDouble(beatElement, 0.35, "transitionDuration", "transitionDurationSec"), 0.0, 1.5),
                            DurationWeight = Math.Clamp(GetDouble(beatElement, 1.0, "durationWeight", "weight"), 0.25, 4.0),
                            OverlayText = NormalizeText(GetStr(beatElement, "overlayText", "onScreenText", "text"), sourceImage?.OverlayText ?? ""),
                            CutReason = NormalizeText(GetStr(beatElement, "cutReason", "reason"), ""),
                            Emphasis = NormalizeText(GetStr(beatElement, "emphasis", "emphasisKeywords"), ""),
                            ShotType = NormalizeText(GetStr(beatElement, "shotType", "shot"), sourceImage?.ShotType ?? ""),
                            Composition = NormalizeText(GetStr(beatElement, "composition", "framing"), sourceImage?.Composition ?? ""),
                            ContinuityNotes = NormalizeText(GetStr(beatElement, "continuityNotes", "continuity"), sourceImage?.ContinuityAnchor ?? ""),
                            DirectorIntent = NormalizeText(GetStr(beatElement, "directorIntent", "intent"), sourceImage?.DirectorIntent ?? "")
                        });
                        index++;
                    }
                }

                ReadCaptionCues(sceneElement, plan);
                ReadAudioCues(sceneElement, plan);
                EnsureSceneHasPlan(plan, sourceScene, sceneImages, storyboardScene, sttData);

                payload.Scenes.Add(plan);
            }

            foreach (var missingScene in scriptData.Scenes.Where(x => payload.Scenes.All(s => s.SceneNumber != x.SceneNumber)))
            {
                payload.Scenes.Add(BuildFallbackScenePlan(
                    missingScene,
                    imageData.SceneImages.Where(x => x.SceneNumber == missingScene.SceneNumber).ToList(),
                    storyboard?.Scenes.FirstOrDefault(x => x.SceneNumber == missingScene.SceneNumber),
                    sttData,
                    audioDurations));
            }

            payload.Scenes = payload.Scenes.OrderBy(x => x.SceneNumber).ToList();
            return payload;
        }

        private static EditPlanStagePayload BuildFallbackPlan(
            ScriptStagePayload scriptData,
            ImageStagePayload imageData,
            StoryboardStagePayload? storyboard,
            SttStagePayload? sttData,
            IReadOnlyDictionary<int, double> audioDurations)
        {
            return new EditPlanStagePayload
            {
                ScriptId = scriptData.ScriptId,
                PacingProfile = "documentary",
                TransitionPalette = "editorial_cuts",
                CaptionStrategy = sttData?.Subtitles.Count > 0 ? "keyword_emphasis" : "word_sync",
                AudioMood = "Keep voice clear; use subtle emphasis on hooks and reveals.",
                DirectorSummary = storyboard?.EditingPrinciples ?? "Cut on idea changes, preserve visual continuity and refresh attention before static scenes feel flat.",
                VisualContinuityNotes = storyboard?.VisualContinuityBible ?? "Keep palette, motifs and realism consistent while varying shot scale.",
                MusicEnergyCurve = storyboard?.ChapterStrategy ?? "Medium hook, lower explanatory body, controlled lift on reveals and ending.",
                SilenceStrategy = "Use short silence before reveals or chapter breaks; keep narration intelligible.",
                Scenes = scriptData.Scenes
                    .OrderBy(x => x.SceneNumber)
                    .Select(scene => BuildFallbackScenePlan(
                        scene,
                        imageData.SceneImages.Where(img => img.SceneNumber == scene.SceneNumber).ToList(),
                        storyboard?.Scenes.FirstOrDefault(s => s.SceneNumber == scene.SceneNumber),
                        sttData,
                        audioDurations))
                    .ToList()
            };
        }

        private static EditScenePlan BuildFallbackScenePlan(
            ScriptSceneItem scene,
            List<SceneImageItem> sceneImages,
            StoryboardScenePlan? storyboardScene,
            SttStagePayload? sttData,
            IReadOnlyDictionary<int, double> audioDurations)
        {
            var orderedImages = sceneImages
                .Where(x => !string.IsNullOrWhiteSpace(x.ImagePath))
                .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                .ToList();

            var intent = storyboardScene?.SceneType ?? (scene.SceneNumber == 1 ? "hook" : "explanation");
            var duration = audioDurations.TryGetValue(scene.SceneNumber, out var audioDuration)
                ? audioDuration
                : Math.Max(0.25, scene.EstimatedDuration);

            var plan = new EditScenePlan
            {
                SceneNumber = scene.SceneNumber,
                Intent = NormalizeToken(intent, "explanation"),
                Pacing = PickFallbackPacing(intent, duration),
                AudioDurationSec = duration,
                ChapterTitle = storyboardScene?.ChapterTitle ?? "",
                RetentionGoal = storyboardScene?.RetentionGoal ?? "",
                MusicEnergy = NormalizeEnergy(storyboardScene?.MusicEnergy, "medium"),
                CaptionMode = storyboardScene?.CaptionMode ?? "",
                ContinuityAnchor = storyboardScene?.ContinuityAnchor ?? "",
                EditorNote = "Fallback edit plan: use available visual beats with varied motion and clean editorial cuts."
            };

            if (orderedImages.Count == 0)
            {
                orderedImages.Add(new SceneImageItem
                {
                    SceneNumber = scene.SceneNumber,
                    BeatIndex = 1,
                    BeatCount = 1,
                    BeatRole = "primary",
                    EffectType = scene.SceneNumber % 2 == 0 ? "zoom_out" : "zoom_in",
                    TransitionType = storyboardScene?.TransitionType ?? "cut",
                    ImagePath = "",
                    OverlayText = storyboardScene?.OverlayText ?? ""
                });
            }

            var maxBeats = Math.Min(MaxVisualBeatsPerScene, orderedImages.Count);
            for (var i = 0; i < maxBeats; i++)
            {
                var image = orderedImages[i];
                var transition = i == 0
                    ? NormalizeTransition(storyboardScene?.TransitionType ?? image.TransitionType, image.TransitionType)
                    : "cut";

                plan.VisualBeats.Add(new EditVisualBeatPlan
                {
                    BeatIndex = i + 1,
                    SourceImageBeatIndex = image.BeatIndex <= 0 ? i + 1 : image.BeatIndex,
                    ImagePath = image.ImagePath,
                    VisualRole = string.IsNullOrWhiteSpace(image.BeatRole) ? (i == 0 ? "primary" : "broll") : image.BeatRole,
                    SegmentRole = PickFallbackSegmentRole(i, maxBeats, intent),
                    EffectType = NormalizeEffect(image.EffectType, i % 2 == 0 ? "zoom_in" : "pan_left"),
                    TransitionType = NormalizeTransitionForBeat(transition, image.TransitionType, i + 1, intent, PickFallbackSegmentRole(i, maxBeats, intent)),
                    TransitionDuration = transition == "cut" ? 0.0 : 0.35,
                    DurationWeight = i == 0 && intent == "hook" ? 1.35 : 1.0,
                    OverlayText = string.IsNullOrWhiteSpace(image.OverlayText) ? storyboardScene?.OverlayText ?? "" : image.OverlayText,
                    CutReason = i == 0 ? "establish scene" : "refresh visual attention",
                    Emphasis = storyboardScene?.EmotionalTone ?? "",
                    ShotType = image.ShotType,
                    Composition = image.Composition,
                    ContinuityNotes = string.IsNullOrWhiteSpace(image.ContinuityAnchor) ? storyboardScene?.ContinuityAnchor ?? "" : image.ContinuityAnchor,
                    DirectorIntent = string.IsNullOrWhiteSpace(image.DirectorIntent) ? storyboardScene?.ScenePurpose ?? "" : image.DirectorIntent
                });
            }

            AddFallbackCaptionCues(plan, sttData);
            if (intent is "hook" or "reveal")
            {
                plan.AudioCues.Add(new EditAudioCue
                {
                    Type = intent == "hook" ? "low_boom" : "hit",
                    AtSec = 0,
                    Note = "Optional subtle emphasis cue."
                });
            }

            return plan;
        }

        private static void EnsureSceneHasPlan(
            EditScenePlan plan,
            ScriptSceneItem sourceScene,
            List<SceneImageItem> sceneImages,
            StoryboardScenePlan? storyboardScene,
            SttStagePayload? sttData)
        {
            if (plan.VisualBeats.Count == 0 || plan.VisualBeats.All(x => string.IsNullOrWhiteSpace(x.ImagePath)) && sceneImages.Count > 0)
            {
                var fallback = BuildFallbackScenePlan(
                    sourceScene,
                    sceneImages,
                    storyboardScene,
                    sttData,
                    new Dictionary<int, double> { [sourceScene.SceneNumber] = plan.AudioDurationSec });

                plan.VisualBeats = fallback.VisualBeats;
            }

            var orderedImages = sceneImages
                .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                .ToList();

            var targetBeatCount = EstimateEditBeatCount(plan.AudioDurationSec > 0 ? plan.AudioDurationSec : sourceScene.EstimatedDuration, orderedImages.Count);
            if (plan.VisualBeats.Count < targetBeatCount && orderedImages.Count > plan.VisualBeats.Count)
            {
                var fallback = BuildFallbackScenePlan(
                    sourceScene,
                    sceneImages,
                    storyboardScene,
                    sttData,
                    new Dictionary<int, double> { [sourceScene.SceneNumber] = plan.AudioDurationSec });

                var usedSourceBeatIndexes = plan.VisualBeats
                    .Select(x => x.SourceImageBeatIndex)
                    .Where(x => x > 0)
                    .ToHashSet();

                foreach (var fallbackBeat in fallback.VisualBeats.Where(x => !usedSourceBeatIndexes.Contains(x.SourceImageBeatIndex)))
                {
                    if (plan.VisualBeats.Count >= targetBeatCount) break;
                    plan.VisualBeats.Add(CloneBeat(fallbackBeat));
                }

                foreach (var fallbackBeat in fallback.VisualBeats)
                {
                    if (plan.VisualBeats.Count >= targetBeatCount) break;
                    if (plan.VisualBeats.Any(x => x.ImagePath == fallbackBeat.ImagePath && !string.IsNullOrWhiteSpace(x.ImagePath))) continue;
                    plan.VisualBeats.Add(CloneBeat(fallbackBeat));
                }
            }

            for (var i = 0; i < plan.VisualBeats.Count; i++)
            {
                var beat = plan.VisualBeats[i];
                var sourceImage = FindSceneImage(orderedImages, beat.SourceImageBeatIndex, i + 1);

                beat.BeatIndex = i + 1;
                beat.SourceImageBeatIndex = beat.SourceImageBeatIndex <= 0 ? i + 1 : beat.SourceImageBeatIndex;
                beat.ImagePath = string.IsNullOrWhiteSpace(beat.ImagePath) ? sourceImage?.ImagePath ?? "" : beat.ImagePath;
                beat.VisualRole = NormalizeToken(beat.VisualRole, sourceImage?.BeatRole ?? (i == 0 ? "primary" : "broll"));
                beat.SegmentRole = NormalizeSegmentRole(beat.SegmentRole, i + 1, plan.Intent);
                beat.EffectType = NormalizeEffect(beat.EffectType, sourceImage?.EffectType);
                beat.TransitionType = NormalizeTransitionForBeat(beat.TransitionType, sourceImage?.TransitionType, i + 1, plan.Intent, beat.SegmentRole);
                beat.TransitionDuration = Math.Clamp(beat.TransitionDuration, 0.0, 1.5);
                beat.DurationWeight = Math.Clamp(beat.DurationWeight <= 0 ? 1.0 : beat.DurationWeight, 0.25, 4.0);
                beat.ShotType = NormalizeText(beat.ShotType, sourceImage?.ShotType ?? "");
                beat.Composition = NormalizeText(beat.Composition, sourceImage?.Composition ?? "");
                beat.ContinuityNotes = NormalizeText(beat.ContinuityNotes, sourceImage?.ContinuityAnchor ?? storyboardScene?.ContinuityAnchor ?? "");
                beat.DirectorIntent = NormalizeText(beat.DirectorIntent, sourceImage?.DirectorIntent ?? storyboardScene?.ScenePurpose ?? "");
            }

            if (plan.CaptionCues.Count == 0)
                AddFallbackCaptionCues(plan, sttData);
        }

        private static int EstimateEditBeatCount(double audioDurationSec, int availableImageCount)
        {
            if (availableImageCount <= 0) return 1;

            var desired = audioDurationSec switch
            {
                >= 24 => 4,
                >= 16 => 3,
                >= 8 => 2,
                _ => 1
            };

            return Math.Clamp(Math.Min(desired, availableImageCount), 1, MaxVisualBeatsPerScene);
        }

        private static EditVisualBeatPlan CloneBeat(EditVisualBeatPlan beat) =>
            new()
            {
                BeatIndex = beat.BeatIndex,
                SourceImageBeatIndex = beat.SourceImageBeatIndex,
                ImagePath = beat.ImagePath,
                VisualRole = beat.VisualRole,
                SegmentRole = beat.SegmentRole,
                EffectType = beat.EffectType,
                TransitionType = beat.TransitionType,
                TransitionDuration = beat.TransitionDuration,
                DurationWeight = beat.DurationWeight,
                OverlayText = beat.OverlayText,
                CutReason = beat.CutReason,
                Emphasis = beat.Emphasis,
                ShotType = beat.ShotType,
                Composition = beat.Composition,
                ContinuityNotes = beat.ContinuityNotes,
                DirectorIntent = beat.DirectorIntent
            };

        private static SceneImageItem? FindSceneImage(List<SceneImageItem> sceneImages, int sourceBeatIndex, int fallbackIndex)
        {
            if (sceneImages.Count == 0) return null;

            return sceneImages.FirstOrDefault(x => x.BeatIndex == sourceBeatIndex)
                ?? sceneImages.ElementAtOrDefault(Math.Clamp(fallbackIndex - 1, 0, sceneImages.Count - 1))
                ?? sceneImages.FirstOrDefault();
        }

        private static void ReadCaptionCues(JsonElement sceneElement, EditScenePlan plan)
        {
            if (!TryGetProperty(sceneElement, out var cuesElement, "captionCues", "captions", "captionBeats")
                || cuesElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var cueElement in cuesElement.EnumerateArray().Take(6))
            {
                if (cueElement.ValueKind != JsonValueKind.Object) continue;

                var text = GetStr(cueElement, "text", "phrase");
                if (string.IsNullOrWhiteSpace(text)) continue;

                plan.CaptionCues.Add(new EditCaptionCue
                {
                    Text = text,
                    StartSec = Math.Max(0, GetDouble(cueElement, 0, "startSec", "start")),
                    EndSec = Math.Max(0, GetDouble(cueElement, 0, "endSec", "end")),
                    Emphasis = NormalizeText(GetStr(cueElement, "emphasis"), "")
                });
            }
        }

        private static void ReadAudioCues(JsonElement sceneElement, EditScenePlan plan)
        {
            if (!TryGetProperty(sceneElement, out var cuesElement, "audioCues", "sfxCues", "soundCues")
                || cuesElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var cueElement in cuesElement.EnumerateArray().Take(4))
            {
                if (cueElement.ValueKind != JsonValueKind.Object) continue;

                var cueType = NormalizeAudioCueType(GetStr(cueElement, "type", "cue"));
                if (cueType is "none" or "silence")
                    continue;

                plan.AudioCues.Add(new EditAudioCue
                {
                    Type = cueType,
                    AtSec = Math.Max(0, GetDouble(cueElement, 0, "atSec", "at", "time")),
                    Note = NormalizeText(GetStr(cueElement, "note", "description"), "")
                });
            }
        }

        private static void AddFallbackCaptionCues(EditScenePlan plan, SttStagePayload? sttData)
        {
            var words = sttData?.Subtitles
                .Where(x => x.SceneNumber == plan.SceneNumber)
                .OrderBy(x => x.Start)
                .ToList();

            if (words == null || words.Count == 0) return;

            var firstWords = string.Join(" ", words.Take(5).Select(x => x.Word)).Trim();
            if (string.IsNullOrWhiteSpace(firstWords)) return;

            plan.CaptionCues.Add(new EditCaptionCue
            {
                Text = firstWords,
                StartSec = words.First().Start,
                EndSec = words.Take(5).Last().End,
                Emphasis = plan.Intent
            });
        }

        private static string BuildTranscriptDigest(SttStagePayload? sttData, int sceneNumber)
        {
            var words = sttData?.Subtitles
                .Where(x => x.SceneNumber == sceneNumber)
                .OrderBy(x => x.Start)
                .Take(24)
                .Select(x => x.Word)
                .ToList();

            return words == null || words.Count == 0 ? "" : string.Join(" ", words);
        }

        private static string ShortenForPrompt(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return clean.Length <= maxLength ? clean : clean[..Math.Max(0, maxLength - 3)] + "...";
        }

        private static string PickFallbackPacing(string intent, double duration)
        {
            var token = NormalizeToken(intent, "explanation");
            if (token == "hook") return "fast";
            if (token is "reveal" or "proof") return "impact";
            if (duration >= 18) return "balanced";
            return "fast";
        }

        private static string CleanJson(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewLine = text.IndexOf('\n');
                if (firstNewLine >= 0) text = text[(firstNewLine + 1)..];
                if (text.EndsWith("```", StringComparison.Ordinal)) text = text[..^3];
            }

            var firstJsonChar = text.IndexOf('{');
            if (firstJsonChar > 0) text = text[firstJsonChar..];

            var lastJsonChar = text.LastIndexOf('}');
            if (lastJsonChar >= 0 && lastJsonChar < text.Length - 1)
                text = text[..(lastJsonChar + 1)];

            return text.Trim();
        }

        private static bool TryGetProperty(JsonElement el, out JsonElement value, params string[] names)
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in el.EnumerateObject())
                {
                    if (names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string GetStr(JsonElement el, params string[] props)
        {
            if (!TryGetProperty(el, out var p, props)) return "";

            return p.ValueKind switch
            {
                JsonValueKind.String => p.GetString()?.Trim() ?? "",
                JsonValueKind.Number => p.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => ""
            };
        }

        private static int GetInt(JsonElement el, int fallback, params string[] props)
        {
            if (!TryGetProperty(el, out var p, props)) return fallback;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i)) return i;
            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var parsed)) return parsed;

            return fallback;
        }

        private static double GetDouble(JsonElement el, double fallback, params string[] props)
        {
            if (!TryGetProperty(el, out var p, props)) return fallback;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var d)) return d;
            if (p.ValueKind == JsonValueKind.String
                && double.TryParse(p.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static string NormalizeEffect(string? value, string? fallback = null)
        {
            var token = NormalizeToken(value ?? "", "");
            if (string.IsNullOrWhiteSpace(token)) token = NormalizeToken(fallback ?? "", "zoom_in");

            return token switch
            {
                "slow_push_in" or "push_in" or "zoom_in" or "impact_zoom" => "zoom_in",
                "slow_pull_out" or "pull_out" or "zoom_out" => "zoom_out",
                "pan_left" => "pan_left",
                "pan_right" => "pan_right",
                "pan_up" => "pan_up",
                "pan_down" => "pan_down",
                "static" or "static_hold" => "static",
                _ => "zoom_in"
            };
        }

        private static string NormalizeTransition(string? value, string? fallback = null)
        {
            var token = NormalizeToken(value ?? "", "");
            if (string.IsNullOrWhiteSpace(token)) token = NormalizeToken(fallback ?? "", "cut");

            return token switch
            {
                "crossfade" or "fade" => "crossfade",
                "dip_black" or "dip_to_black" => "dip_black",
                "flash" => "flash",
                "match_cut" => "match_cut",
                _ => "cut"
            };
        }

        private static string NormalizeTransitionForBeat(string? value, string? fallback, int beatIndex, string intent, string? segmentRole)
        {
            var transition = NormalizeTransition(value, fallback);
            var role = NormalizeSegmentRole(segmentRole ?? "", beatIndex, intent);
            var normalizedIntent = NormalizeToken(intent, "explanation");

            if (beatIndex <= 1)
            {
                return normalizedIntent is "hook" or "reveal" or "outro"
                    ? transition
                    : transition is "flash" ? "cut" : transition;
            }

            if (role is "detail")
                return transition is "flash" or "dip_black" ? "cut" : transition;

            if (role is "emphasis")
                return transition is "dip_black" ? "crossfade" : transition;

            if (role is "transition")
                return transition is "flash" ? "dip_black" : transition;

            if (role is "payoff")
                return normalizedIntent is "reveal" or "proof" ? transition : transition is "flash" ? "cut" : transition;

            return transition;
        }

        private static string NormalizeSegmentRole(string? value, int beatIndex, string intent)
        {
            var token = NormalizeToken(value ?? "", "");
            if (string.IsNullOrWhiteSpace(token))
                return PickFallbackSegmentRole(beatIndex - 1, Math.Max(beatIndex, 1), intent);

            return token switch
            {
                "establish" or "establishing" or "setup" or "primary" => "establishing",
                "detail" or "evidence" or "proof" or "example" or "broll" => "detail",
                "emphasis" or "impact" or "highlight" or "beat" => "emphasis",
                "transition" or "bridge" or "chapter_transition" or "chapter_card" => "transition",
                "payoff" or "reveal" or "resolution" => "payoff",
                _ => PickFallbackSegmentRole(beatIndex - 1, Math.Max(beatIndex, 1), intent)
            };
        }

        private static string PickFallbackSegmentRole(int zeroBasedIndex, int totalBeats, string intent)
        {
            var normalizedIntent = NormalizeToken(intent, "explanation");
            if (zeroBasedIndex <= 0)
                return "establishing";

            if (zeroBasedIndex == totalBeats - 1 && normalizedIntent is "reveal" or "recap" or "outro")
                return "payoff";

            if (zeroBasedIndex == totalBeats - 1 && normalizedIntent is "context" or "setup")
                return "transition";

            if (normalizedIntent is "proof" or "example")
                return "detail";

            if (normalizedIntent is "hook" or "reveal")
                return "emphasis";

            return zeroBasedIndex % 2 == 0 ? "emphasis" : "detail";
        }

        private static string NormalizePacing(string? value)
        {
            var token = NormalizeToken(value ?? "", "balanced");
            return token switch
            {
                "fast" or "balanced" or "slow" or "impact" => token,
                _ => "balanced"
            };
        }

        private static string NormalizeAudioCueType(string? value)
        {
            var token = NormalizeToken(value ?? "", "none");
            return token switch
            {
                "hit" or "impact" or "stab" => "hit",
                "whoosh" or "swoosh" or "swish" => "whoosh",
                "low_boom" or "boom" or "sub_boom" or "cinematic_boom" => "low_boom",
                "silence" or "pause" => "silence",
                "none" or "no" or "off" or "sfx" or "sound_effect" or "sound_fx" or "effect" or "music" => "none",
                _ => "none"
            };
        }

        private static string NormalizeEnergy(string? value, string? fallback = null)
        {
            var token = NormalizeToken(value ?? "", NormalizeToken(fallback ?? "", "medium"));
            return token switch
            {
                "low" or "medium" or "high" or "silence" => token,
                _ => "medium"
            };
        }

        private static string NormalizeText(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static string NormalizeToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }
}
