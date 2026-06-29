using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Application.Services;
using Core.Attributes;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using System.Text.Json;

namespace Application.Executors
{
    [StageExecutor(StageType.Storyboard)]
    public class StoryboardStageExecutor : BaseStageExecutor
    {
        private const int MaxBeatsPerScene = 3;
        private const int MaxChapters = 8;

        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IRepository<ScriptPreset> _scriptPresetRepo;

        public StoryboardStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IRepository<ScriptPreset> scriptPresetRepo)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _scriptPresetRepo = scriptPresetRepo;
        }

        public override StageType StageType => StageType.Storyboard;

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
            if (scriptData == null || scriptData.Scenes == null || scriptData.Scenes.Count == 0)
                throw new InvalidOperationException("Storyboard için Script çıktısı bulunamadı.");

            await logAsync($"Storyboard hazırlanıyor. Sahne sayısı: {scriptData.Scenes.Count}.");
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);
            if (conceptProfile != null)
                await logAsync($"Storyboard konsept gorsel kimligini kullanacak. Stil: {PipelineLiveLog.Shorten(conceptProfile.VisualStyleName, 120)}");

            CreativeDirectorStagePayload? creativePlan = null;
            if (context.HasOutput(StageType.CreativeDirector))
            {
                creativePlan = context.GetOutput<CreativeDirectorStagePayload>(StageType.CreativeDirector);
                await logAsync($"Storyboard Creative Director planini kullanacak. Vaad: {PipelineLiveLog.Shorten(creativePlan.VideoPromise, 160)}");
            }

            var scriptPreset = await TryResolveScriptPresetAsync(run, ct);
            if (scriptPreset == null)
            {
                await logAsync(PipelineLiveLog.Warning("Script preset bulunamadı. Storyboard deterministik yönetmen planı ile üretilecek."));
                return BuildFallbackStoryboard(scriptData, conceptProfile);
            }

            try
            {
                var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, scriptPreset.UserAiConnectionId, ct);
                var prompt = BuildStoryboardPrompt(scriptData, scriptPreset, creativePlan, conceptProfile);

                await logAsync($"AI storyboard isteği gönderiliyor. Model: {scriptPreset.ModelName}.");
                var response = await aiClient.GenerateTextAsync(
                    prompt,
                    temperature: Math.Min(0.95, Math.Max(0.55, 0.72)),
                    model: scriptPreset.ModelName,
                    ct: ct);

                var storyboard = ParseStoryboard(response, scriptData, conceptProfile);
                await logAsync(PipelineLiveLog.Success($"Storyboard üretildi. Chapter: {storyboard.Chapters.Count}, visual beat: {storyboard.Scenes.Sum(x => x.VisualBeats.Count)}."));
                return storyboard;
            }
            catch (Exception ex)
            {
                await logAsync(PipelineLiveLog.Warning($"AI storyboard üretimi başarısız oldu. Fallback yönetmen planı kullanılacak. Hata: {PipelineLiveLog.Shorten(ex.Message, 280)}"));
                return BuildFallbackStoryboard(scriptData, conceptProfile);
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

        private static string BuildStoryboardPrompt(
            ScriptStagePayload script,
            ScriptPreset scriptPreset,
            CreativeDirectorStagePayload? creativePlan,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var scenes = script.Scenes
                .OrderBy(x => x.SceneNumber)
                .Select(x => new
                {
                    sceneNumber = x.SceneNumber,
                    estimatedDuration = x.EstimatedDuration,
                    sceneRole = x.SceneRole,
                    scenePurpose = x.ScenePurpose,
                    viewerQuestion = x.ViewerQuestion,
                    emotionalBeat = x.EmotionalBeat,
                    visualType = x.VisualType,
                    visualVarietyRole = x.VisualVarietyRole,
                    visualVarietyReason = x.VisualVarietyReason,
                    cameraPlan = x.CameraPlan,
                    overlayText = x.OverlayText,
                    sfxCue = x.SfxCue,
                    transitionIntent = x.TransitionIntent,
                    chapterTitle = x.ChapterTitle,
                    visual = x.VisualPrompt,
                    narration = x.AudioText
                });

            var sceneJson = JsonSerializer.Serialize(scenes);

            return $$"""
You are the director, cinematographer and human retention editor for a long-form YouTube video.
Create a Director Layer v2 plan from the script scenes. This plan will drive image generation, edit decisions and render timing.

Video title: {{script.Title}}
Language/culture: {{ProductionPromptContext.ResolveLanguage(conceptProfile, scriptPreset.Language)}}
Tone: {{ProductionPromptContext.ResolveTone(conceptProfile, scriptPreset.Tone)}}

{{(creativePlan == null ? "" : creativePlan.ToPromptBlock())}}

{{ProductionPromptContext.BuildStoryboardContextBlock(conceptProfile)}}

Rules:
- Return ONLY raw JSON. No markdown, no code fences.
- If a Creative Director plan is provided, align chapters, visual motifs, pacing and retention goals with it.
- Keep the same sceneNumber values.
- Build 3 to {{MaxChapters}} chapters unless the script is very short.
- For each scene create 1 to {{MaxBeatsPerScene}} visualBeats.
- Use more beats for longer or more important scenes, fewer beats for short scenes.
- For scenes around 8 seconds or longer, create at least 2 visualBeats. For scenes around 16 seconds or longer, create 3 visualBeats.
- Avoid repetitive prompts. Vary shotType, distance, subject, composition, cameraMotion and emotionalTone.
- Keep one coherent visual identity across the video: recurring motifs, color palette, lens language, lighting and subject rules.
- Prompts must be image-generation ready and follow the concept text policy. Do not request text unless the policy or scene explicitly calls for it.
- Add concrete negative visual rules to prevent generic stock imagery, text, logos, distorted hands/faces and inconsistent characters.
- scenePurpose and retentionGoal must explain why the scene exists and what attention problem it solves.
- Use script scene direction fields when present: sceneRole, scenePurpose, viewerQuestion, emotionalBeat, visualType, visualVarietyRole, visualVarietyReason, cameraPlan, overlayText, sfxCue and transitionIntent.
- cutIntent must explain why the edit moves to that beat.
- onScreenText should be short, optional and separate from visualPrompt.

JSON shape:
{
  "directorVersion": "v2",
  "styleBible": "coherent visual style, color palette, lens language, lighting",
  "videoMood": "short overall mood",
  "visualContinuityBible": "rules for recurring motifs, realism, subject consistency and visual variety",
  "colorPalette": "specific colors and contrast rules",
  "cameraLanguage": "lens, framing and camera movement rules",
  "lightingStyle": "lighting rules",
  "editingPrinciples": "how cuts, pauses and emphasis should feel",
  "negativeVisualRules": "things the image generator must avoid",
  "chapterStrategy": "how chapters create progression",
  "chapters": [
    {
      "chapterIndex": 1,
      "title": "short chapter title",
      "purpose": "why this chapter exists",
      "startSceneNumber": 1,
      "endSceneNumber": 3,
      "pacing": "fast | balanced | slow | impact",
      "visualMotif": "recurring visual idea",
      "musicEnergy": "low | medium | high | silence"
    }
  ],
  "scenes": [
    {
      "sceneNumber": 1,
      "chapterIndex": 1,
      "chapterTitle": "short title",
      "sceneType": "hook | explanation | example | reveal | proof | recap | cliffhanger",
      "scenePurpose": "why this scene matters",
      "retentionGoal": "attention problem solved by this scene",
      "emotionalTone": "mystery | tension | wonder | urgency | calm | reveal",
      "visualContrast": "how this scene differs from previous scenes",
      "continuityAnchor": "recurring object, location, symbol, color or motif",
      "musicEnergy": "low | medium | high | silence",
      "captionMode": "word_sync | keyword_emphasis | calm_subtitles",
      "transitionType": "cut | crossfade | dip_black | flash | match_cut",
      "overlayText": "short phrase or empty",
      "soundCue": "hit | whoosh | low_boom | silence | none",
      "visualBeats": [
        {
          "beatIndex": 1,
          "beatRole": "primary | detail | broll | contrast | reveal",
          "shotType": "wide shot | close-up | macro detail | top-down | diagram-like composition",
          "cameraMotion": "slow_push_in | slow_pull_out | pan_left | pan_right | static_hold",
          "subject": "main visible subject",
          "composition": "framing and spatial layout",
          "lens": "lens / depth of field direction",
          "lighting": "lighting direction",
          "colorNotes": "palette notes for this beat",
          "continuityNotes": "how this beat connects to the visual bible",
          "negativePrompt": "beat-specific avoid list",
          "cutIntent": "why this cut exists",
          "visualPrompt": "specific cinematic image prompt",
          "onScreenText": "short phrase or empty",
          "durationWeight": 1.0
        }
      ]
    }
  ]
}

Script scenes:
{{sceneJson}}
""";
        }

        private static StoryboardStagePayload ParseStoryboard(
            string response,
            ScriptStagePayload scriptData,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var cleanJson = CleanJson(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var payload = new StoryboardStagePayload
            {
                ScriptId = scriptData.ScriptId,
                DirectorVersion = NormalizeText(GetStr(root, "directorVersion", "version"), "v2"),
                StyleBible = GetStr(root, "styleBible", "style", "visualStyle"),
                VideoMood = GetStr(root, "videoMood", "mood"),
                VisualContinuityBible = GetStr(root, "visualContinuityBible", "continuityBible", "visualRules"),
                ColorPalette = GetStr(root, "colorPalette", "palette"),
                CameraLanguage = GetStr(root, "cameraLanguage", "cameraRules", "lensLanguage"),
                LightingStyle = GetStr(root, "lightingStyle", "lighting"),
                EditingPrinciples = GetStr(root, "editingPrinciples", "editRules"),
                NegativeVisualRules = GetStr(root, "negativeVisualRules", "negativePrompt", "avoid"),
                ChapterStrategy = GetStr(root, "chapterStrategy", "chapterPlan")
            };
            payload.Chapters = ReadChapters(root, scriptData.Scenes);

            if (!TryGetProperty(root, out var scenesElement, "scenes", "items") || scenesElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Storyboard JSON içinde scenes array bulunamadı.");

            foreach (var sceneElement in scenesElement.EnumerateArray())
            {
                if (sceneElement.ValueKind != JsonValueKind.Object) continue;

                var sceneNumber = GetInt(sceneElement, 0, "sceneNumber", "scene", "number");
                var sourceScene = scriptData.Scenes.FirstOrDefault(x => x.SceneNumber == sceneNumber);
                if (sourceScene == null) continue;

                var plan = new StoryboardScenePlan
                {
                    SceneNumber = sceneNumber,
                    ChapterIndex = Math.Max(1, GetInt(sceneElement, FindChapterIndex(payload.Chapters, sceneNumber), "chapterIndex", "chapter")),
                    ChapterTitle = NormalizeText(GetStr(sceneElement, "chapterTitle"), sourceScene.ChapterTitle),
                    SceneType = NormalizeToken(GetStr(sceneElement, "sceneType", "type"), FirstNonEmpty(sourceScene.SceneRole, "explanation")),
                    ScenePurpose = NormalizeText(GetStr(sceneElement, "scenePurpose", "purpose"), sourceScene.ScenePurpose),
                    RetentionGoal = NormalizeText(GetStr(sceneElement, "retentionGoal", "retention"), sourceScene.ViewerQuestion),
                    EmotionalTone = NormalizeToken(GetStr(sceneElement, "emotionalTone", "tone", "mood"), FirstNonEmpty(sourceScene.EmotionalBeat, "curious")),
                    VisualContrast = NormalizeText(GetStr(sceneElement, "visualContrast", "contrast"), ""),
                    ContinuityAnchor = NormalizeText(GetStr(sceneElement, "continuityAnchor", "anchor", "motif"), ""),
                    MusicEnergy = NormalizeEnergy(GetStr(sceneElement, "musicEnergy", "energy")),
                    CaptionMode = NormalizeToken(GetStr(sceneElement, "captionMode", "captionStrategy"), "word_sync"),
                    TransitionType = NormalizeTransition(FirstNonEmpty(GetStr(sceneElement, "transitionType", "transition"), sourceScene.TransitionIntent)),
                    OverlayText = FirstNonEmpty(GetStr(sceneElement, "overlayText", "onScreenText", "text"), sourceScene.OverlayText),
                    SoundCue = NormalizeToken(FirstNonEmpty(GetStr(sceneElement, "soundCue", "sfx"), sourceScene.SfxCue), "none")
                };
                if (string.IsNullOrWhiteSpace(plan.ChapterTitle))
                    plan.ChapterTitle = payload.Chapters.FirstOrDefault(x => x.ChapterIndex == plan.ChapterIndex)?.Title ?? "";

                if (TryGetProperty(sceneElement, out var beatsElement, "visualBeats", "beats", "shots")
                    && beatsElement.ValueKind == JsonValueKind.Array)
                {
                    var index = 1;
                    foreach (var beatElement in beatsElement.EnumerateArray().Take(MaxBeatsPerScene))
                    {
                        if (beatElement.ValueKind != JsonValueKind.Object) continue;

                        var prompt = GetStr(beatElement, "visualPrompt", "prompt", "imagePrompt", "visual");
                        if (string.IsNullOrWhiteSpace(prompt)) prompt = sourceScene.VisualPrompt;

                        plan.VisualBeats.Add(new StoryboardVisualBeat
                        {
                            BeatIndex = GetInt(beatElement, index, "beatIndex", "index", "number"),
                            BeatRole = NormalizeToken(GetStr(beatElement, "beatRole", "role"), index == 1 ? "primary" : "broll"),
                            ShotType = NormalizeText(GetStr(beatElement, "shotType", "shot"), DefaultShotType(sourceScene.VisualType)),
                            CameraMotion = NormalizeMotion(FirstNonEmpty(GetStr(beatElement, "cameraMotion", "motion", "movement"), sourceScene.CameraPlan)),
                            Subject = NormalizeText(GetStr(beatElement, "subject"), ""),
                            Composition = NormalizeText(GetStr(beatElement, "composition", "framing"), ""),
                            Lens = NormalizeText(GetStr(beatElement, "lens", "focalLength"), ""),
                            Lighting = NormalizeText(GetStr(beatElement, "lighting", "light"), ""),
                            ColorNotes = NormalizeText(GetStr(beatElement, "colorNotes", "color"), ""),
                            ContinuityNotes = NormalizeText(GetStr(beatElement, "continuityNotes", "continuity"), ""),
                            NegativePrompt = NormalizeText(GetStr(beatElement, "negativePrompt", "avoid"), ""),
                            CutIntent = NormalizeText(GetStr(beatElement, "cutIntent", "cutReason"), ""),
                            VisualPrompt = prompt.Trim(),
                            OnScreenText = GetStr(beatElement, "onScreenText", "overlayText", "text"),
                            DurationWeight = Math.Clamp(GetDouble(beatElement, 1.0, "durationWeight", "weight"), 0.25, 4.0)
                        });
                        index++;
                    }
                }

                EnsureSceneHasBeats(plan, sourceScene, payload.StyleBible);
                payload.Scenes.Add(plan);
            }

            foreach (var scriptScene in scriptData.Scenes.Where(x => payload.Scenes.All(s => s.SceneNumber != x.SceneNumber)))
            {
                var chapter = payload.Chapters.FirstOrDefault(x => scriptScene.SceneNumber >= x.StartSceneNumber && scriptScene.SceneNumber <= x.EndSceneNumber);
                payload.Scenes.Add(BuildFallbackScenePlan(scriptScene, payload.StyleBible, chapter));
            }

            payload.Scenes = payload.Scenes.OrderBy(x => x.SceneNumber).ToList();
            if (string.IsNullOrWhiteSpace(payload.StyleBible))
                payload.StyleBible = FirstNonEmpty(conceptProfile?.VisualStyleBible, "cinematic documentary, rich contrast, motivated lighting, varied camera distance, coherent color palette");
            if (string.IsNullOrWhiteSpace(payload.VisualContinuityBible))
                payload.VisualContinuityBible = FirstNonEmpty(conceptProfile?.CharacterBible, payload.StyleBible);
            if (string.IsNullOrWhiteSpace(payload.ColorPalette))
                payload.ColorPalette = "deep neutrals, cinematic contrast, controlled accent color";
            if (string.IsNullOrWhiteSpace(payload.CameraLanguage))
                payload.CameraLanguage = "varied wide, medium and close shots with slow motivated movement";
            if (string.IsNullOrWhiteSpace(payload.LightingStyle))
                payload.LightingStyle = "motivated soft-key lighting with practical highlights";
            if (string.IsNullOrWhiteSpace(payload.NegativeVisualRules))
                payload.NegativeVisualRules = FirstNonEmpty(
                    string.IsNullOrWhiteSpace(conceptProfile?.TextPolicy)
                        ? null
                        : "avoid random or unrequested text, logos, watermark, generic stock look and inconsistent character identity",
                    "avoid text, logos, watermark, generic stock photo look, distorted anatomy, inconsistent character identity");
            if (payload.Chapters.Count == 0)
                payload.Chapters = BuildFallbackChapters(scriptData.Scenes);

            return payload;
        }

        private static List<DirectorChapterPlan> ReadChapters(JsonElement root, List<ScriptSceneItem> scenes)
        {
            if (!TryGetProperty(root, out var chaptersElement, "chapters", "chapterPlans", "sections")
                || chaptersElement.ValueKind != JsonValueKind.Array)
            {
                return new List<DirectorChapterPlan>();
            }

            var result = new List<DirectorChapterPlan>();
            var index = 1;
            var maxSceneNumber = scenes.Count == 0 ? 1 : scenes.Max(x => x.SceneNumber);

            foreach (var chapterElement in chaptersElement.EnumerateArray().Take(MaxChapters))
            {
                if (chapterElement.ValueKind != JsonValueKind.Object) continue;

                var startScene = Math.Clamp(GetInt(chapterElement, index, "startSceneNumber", "startScene", "from"), 1, maxSceneNumber);
                var endScene = Math.Clamp(GetInt(chapterElement, startScene, "endSceneNumber", "endScene", "to"), startScene, maxSceneNumber);

                result.Add(new DirectorChapterPlan
                {
                    ChapterIndex = Math.Max(1, GetInt(chapterElement, index, "chapterIndex", "index", "number")),
                    Title = NormalizeText(GetStr(chapterElement, "title", "name"), $"Chapter {index}"),
                    Purpose = NormalizeText(GetStr(chapterElement, "purpose", "intent"), ""),
                    StartSceneNumber = startScene,
                    EndSceneNumber = endScene,
                    Pacing = NormalizePacing(GetStr(chapterElement, "pacing", "tempo")),
                    VisualMotif = NormalizeText(GetStr(chapterElement, "visualMotif", "motif"), ""),
                    MusicEnergy = NormalizeEnergy(GetStr(chapterElement, "musicEnergy", "energy"))
                });
                index++;
            }

            return result;
        }

        private static List<DirectorChapterPlan> BuildFallbackChapters(List<ScriptSceneItem> scenes)
        {
            if (scenes.Count == 0)
                return new List<DirectorChapterPlan>();

            var ordered = scenes.OrderBy(x => x.SceneNumber).ToList();
            var chapterCount = Math.Clamp((int)Math.Ceiling(ordered.Count / 4.0), 1, Math.Min(MaxChapters, ordered.Count));
            var chapterSize = (int)Math.Ceiling(ordered.Count / (double)chapterCount);
            var titles = new[] { "Hook", "Context", "Deep Dive", "Proof", "Payoff", "Recap", "Outro", "Afterthought" };
            var motifs = new[] { "central question", "evidence wall", "close detail", "timeline motif", "contrast image", "quiet resolution" };
            var chapters = new List<DirectorChapterPlan>();

            for (var i = 0; i < chapterCount; i++)
            {
                var group = ordered.Skip(i * chapterSize).Take(chapterSize).ToList();
                if (group.Count == 0) break;

                chapters.Add(new DirectorChapterPlan
                {
                    ChapterIndex = i + 1,
                    Title = titles[Math.Min(i, titles.Length - 1)],
                    Purpose = i == 0 ? "Earn attention and frame the main question." : "Move the viewer to the next layer of understanding.",
                    StartSceneNumber = group.First().SceneNumber,
                    EndSceneNumber = group.Last().SceneNumber,
                    Pacing = i == 0 ? "fast" : i == chapterCount - 1 ? "impact" : "balanced",
                    VisualMotif = motifs[i % motifs.Length],
                    MusicEnergy = i == 0 ? "medium" : i == chapterCount - 1 ? "high" : "low"
                });
            }

            return chapters;
        }

        private static int FindChapterIndex(List<DirectorChapterPlan> chapters, int sceneNumber)
            => chapters.FirstOrDefault(x => sceneNumber >= x.StartSceneNumber && sceneNumber <= x.EndSceneNumber)?.ChapterIndex ?? 1;

        private static StoryboardStagePayload BuildFallbackStoryboard(
            ScriptStagePayload scriptData,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var styleBible = FirstNonEmpty(
                conceptProfile?.VisualStyleBible,
                conceptProfile?.VisualStyleName,
                "cinematic documentary, rich contrast, motivated lighting, varied shot distances, coherent color palette, realistic detail");
            var continuityBible = FirstNonEmpty(
                conceptProfile?.CharacterBible,
                "Use coherent documentary realism, recurring symbolic objects, varied shot scale, no random text inside images, no generic stock look.");
            var chapters = BuildFallbackChapters(scriptData.Scenes);

            return new StoryboardStagePayload
            {
                ScriptId = scriptData.ScriptId,
                DirectorVersion = "v2",
                StyleBible = styleBible,
                VideoMood = "curious cinematic documentary with controlled tension",
                VisualContinuityBible = continuityBible,
                ColorPalette = "deep charcoal, warm practical highlights, restrained blue/amber accents, natural skin tones",
                CameraLanguage = "alternate establishing wides, close details and slow push-ins; avoid identical framing across consecutive scenes",
                LightingStyle = "motivated practical lighting, soft key, rich but readable contrast",
                EditingPrinciples = "open with a strong hook, cut on conceptual shifts, use pauses before reveals and avoid decorative transitions",
                NegativeVisualRules = "random or unrequested text, watermark, logo, distorted hands, distorted faces, unreadable diagrams, generic stock photo, inconsistent character identity",
                ChapterStrategy = "Group scenes into clear sections so each chapter has a distinct question, escalation and payoff.",
                Chapters = chapters,
                Scenes = scriptData.Scenes
                    .OrderBy(x => x.SceneNumber)
                    .Select(x => BuildFallbackScenePlan(x, styleBible, chapters.FirstOrDefault(c => x.SceneNumber >= c.StartSceneNumber && x.SceneNumber <= c.EndSceneNumber)))
                    .ToList()
            };
        }

        private static StoryboardScenePlan BuildFallbackScenePlan(ScriptSceneItem scene, string styleBible, DirectorChapterPlan? chapter = null)
        {
            var beatCount = EstimateVisualBeatCount(scene);
            var plan = new StoryboardScenePlan
            {
                SceneNumber = scene.SceneNumber,
                ChapterIndex = chapter?.ChapterIndex ?? 1,
                ChapterTitle = FirstNonEmpty(scene.ChapterTitle, chapter?.Title),
                SceneType = FirstNonEmpty(scene.SceneRole, scene.SceneNumber == 1 ? "hook" : "explanation"),
                ScenePurpose = FirstNonEmpty(scene.ScenePurpose, scene.SceneNumber == 1 ? "Create curiosity and promise the payoff." : "Advance the explanation with a fresh visual angle."),
                RetentionGoal = FirstNonEmpty(scene.ViewerQuestion, scene.SceneNumber == 1 ? "Stop the scroll and make the central question unavoidable." : "Refresh attention before the narration becomes too static."),
                EmotionalTone = FirstNonEmpty(scene.EmotionalBeat, scene.SceneNumber == 1 ? "mystery" : "curious"),
                VisualContrast = FirstNonEmpty(scene.VisualVarietyReason, scene.SceneNumber % 2 == 0 ? "switch to detail or symbolic composition" : "return to broader context"),
                ContinuityAnchor = chapter?.VisualMotif ?? "recurring documentary motif",
                MusicEnergy = chapter?.MusicEnergy ?? (scene.SceneNumber == 1 ? "medium" : "low"),
                CaptionMode = scene.SceneNumber == 1 ? "keyword_emphasis" : "word_sync",
                TransitionType = NormalizeTransition(FirstNonEmpty(scene.TransitionIntent, scene.SceneNumber % 5 == 0 ? "dip_black" : "cut")),
                OverlayText = scene.OverlayText,
                SoundCue = FirstNonEmpty(scene.SfxCue, scene.SceneNumber == 1 ? "low_boom" : "none")
            };

            var primaryShot = DefaultShotType(scene.VisualType);
            var shotTypes = new[] { primaryShot, "close-up detail", "symbolic b-roll composition" };
            var primaryMotion = NormalizeMotion(scene.CameraPlan);
            var motions = new[] { primaryMotion, "pan_left", "slow_pull_out" };
            var roles = new[] { FirstNonEmpty(scene.VisualVarietyRole, "primary"), "detail", "broll" };

            for (var i = 0; i < beatCount; i++)
            {
                plan.VisualBeats.Add(new StoryboardVisualBeat
                {
                    BeatIndex = i + 1,
                    BeatRole = roles[i],
                    ShotType = shotTypes[i],
                    CameraMotion = motions[i],
                    Subject = i == 0 ? "main idea of the narration" : "supporting visual evidence",
                    Composition = i == 0 ? "clean subject-led composition with readable negative space" : "off-center detail composition to create visual refresh",
                    Lens = i == 0 ? "35mm documentary lens" : "50mm shallow depth of field",
                    Lighting = "motivated cinematic soft light",
                    ColorNotes = chapter?.VisualMotif ?? "keep palette consistent with the video color bible",
                    ContinuityNotes = "connect to the chapter motif while changing scale or angle",
                    NegativePrompt = "text, watermark, logo, distorted hands, generic stock photo",
                    CutIntent = i == 0 ? FirstNonEmpty(scene.VisualVarietyReason, "establish the scene idea") : "refresh attention with a new angle",
                    VisualPrompt = $"{scene.VisualPrompt}, visual format: {scene.VisualType}, variety role: {scene.VisualVarietyRole}, {shotTypes[i]}, {styleBible}",
                    DurationWeight = i == 0 ? 1.2 : 1.0
                });
            }

            return plan;
        }

        private static void EnsureSceneHasBeats(StoryboardScenePlan plan, ScriptSceneItem sourceScene, string styleBible)
        {
            if (plan.VisualBeats.Count == 0)
            {
                var fallback = BuildFallbackScenePlan(sourceScene, styleBible);
                plan.VisualBeats.AddRange(fallback.VisualBeats);
            }

            var targetBeatCount = EstimateVisualBeatCount(sourceScene);
            if (plan.VisualBeats.Count < targetBeatCount)
            {
                var fallback = BuildFallbackScenePlan(sourceScene, styleBible);
                foreach (var beat in fallback.VisualBeats.Skip(plan.VisualBeats.Count).Take(targetBeatCount - plan.VisualBeats.Count))
                    plan.VisualBeats.Add(CloneBeat(beat));
            }

            for (var i = 0; i < plan.VisualBeats.Count; i++)
            {
                var beat = plan.VisualBeats[i];
                beat.BeatIndex = i + 1;
                beat.BeatRole = NormalizeToken(beat.BeatRole, i == 0 ? "primary" : "broll");
                beat.CameraMotion = NormalizeMotion(beat.CameraMotion);
                beat.ShotType = NormalizeText(beat.ShotType, "cinematic shot");
                if (string.IsNullOrWhiteSpace(beat.VisualPrompt))
                    beat.VisualPrompt = sourceScene.VisualPrompt;
                beat.Subject = NormalizeText(beat.Subject, "main narration idea");
                beat.Composition = NormalizeText(beat.Composition, i == 0 ? "clear subject-led composition" : "contrasting detail composition");
                beat.Lens = NormalizeText(beat.Lens, i == 0 ? "35mm documentary lens" : "50mm shallow depth of field");
                beat.Lighting = NormalizeText(beat.Lighting, "motivated cinematic lighting");
                beat.ColorNotes = NormalizeText(beat.ColorNotes, "match the visual continuity palette");
                beat.ContinuityNotes = NormalizeText(beat.ContinuityNotes, "keep recurring motif and realism consistent");
                beat.NegativePrompt = NormalizeText(beat.NegativePrompt, "text, watermark, logo, generic stock photo");
                beat.CutIntent = NormalizeText(beat.CutIntent, i == 0 ? "establish the scene idea" : "refresh attention with a new angle");
                beat.DurationWeight = Math.Clamp(beat.DurationWeight <= 0 ? 1.0 : beat.DurationWeight, 0.25, 4.0);
            }
        }

        private static int EstimateVisualBeatCount(ScriptSceneItem scene)
        {
            var duration = Math.Max(scene.EstimatedDuration, EstimateDurationFromNarration(scene.AudioText));
            if (duration >= 16) return 3;
            if (duration >= 8) return 2;
            return 1;
        }

        private static int EstimateDurationFromNarration(string? audioText)
        {
            if (string.IsNullOrWhiteSpace(audioText)) return 0;

            var wordCount = audioText
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Length;

            return (int)Math.Ceiling(wordCount * 60.0 / 155.0);
        }

        private static StoryboardVisualBeat CloneBeat(StoryboardVisualBeat beat) =>
            new()
            {
                BeatIndex = beat.BeatIndex,
                BeatRole = beat.BeatRole,
                ShotType = beat.ShotType,
                CameraMotion = beat.CameraMotion,
                Subject = beat.Subject,
                Composition = beat.Composition,
                Lens = beat.Lens,
                Lighting = beat.Lighting,
                ColorNotes = beat.ColorNotes,
                ContinuityNotes = beat.ContinuityNotes,
                NegativePrompt = beat.NegativePrompt,
                CutIntent = beat.CutIntent,
                VisualPrompt = beat.VisualPrompt,
                OnScreenText = beat.OnScreenText,
                DurationWeight = beat.DurationWeight
            };

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

        private static string NormalizeMotion(string value)
        {
            var token = NormalizeToken(value, "slow_push_in");
            return token switch
            {
                "push_in" or "zoom_in" or "slow_push_in" => "slow_push_in",
                "pull_out" or "zoom_out" or "slow_pull_out" => "slow_pull_out",
                "pan_left" => "pan_left",
                "pan_right" => "pan_right",
                "pan_up" => "pan_up",
                "pan_down" => "pan_down",
                "static" or "static_hold" => "static_hold",
                _ => "slow_push_in"
            };
        }

        private static string NormalizeTransition(string value)
        {
            var token = NormalizeToken(value, "cut");
            return token switch
            {
                "crossfade" or "fade" => "crossfade",
                "dip_black" or "dip_to_black" => "dip_black",
                "flash" => "flash",
                "match_cut" => "match_cut",
                _ => "cut"
            };
        }

        private static string NormalizePacing(string value)
        {
            var token = NormalizeToken(value, "balanced");
            return token switch
            {
                "fast" or "balanced" or "slow" or "impact" => token,
                _ => "balanced"
            };
        }

        private static string NormalizeEnergy(string value)
        {
            var token = NormalizeToken(value, "medium");
            return token switch
            {
                "low" or "medium" or "high" or "silence" => token,
                _ => "medium"
            };
        }

        private static string DefaultShotType(string visualType)
        {
            var token = NormalizeToken(visualType, "cinematic_image");
            return token switch
            {
                "timeline" => "timeline composition",
                "map" => "top-down map-like composition",
                "diagram" => "diagram-like composition",
                "quote_card" => "quote card background",
                "comparison" => "split comparison composition",
                "text_card" => "minimal text-card-safe background",
                "broll" => "close-up b-roll detail",
                _ => "cinematic shot"
            };
        }

        private static string NormalizeText(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static string NormalizeToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";
    }
}
