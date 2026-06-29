using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Application.Services;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.Image)]
    [StagePreset(typeof(ImagePreset))]
    public class ImageStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IUserDirectoryService _dirService;

        public ImageStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IUserDirectoryService dirService)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _dirService = dirService;
        }

        public override StageType StageType => StageType.Image;

        // 🔥 DÜZELTME 1: Access Modifier 'protected override' olmalı (Base sınıf öyle istiyor)
        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync, // 🔥 Bu fonksiyonu kullanacağız
            CancellationToken ct)
        {
            var preset = (ImagePreset)presetObj!;

            // 🔥 DÜZELTME 2: exec.AddLog yerine logAsync kullanıyoruz
            await logAsync($"Görsel üretimi hazırlanıyor. Preset: {preset.Name}, model: {preset.ModelName}, boyut: {preset.Size}.");

            // 1. Önceki Adımdan (Script) Veriyi Çek
            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            if (scriptData == null || scriptData.Scenes == null || !scriptData.Scenes.Any())
                throw new InvalidOperationException("Script verisi bulunamadı veya sahneler boş.");

            await logAsync($"Görsel üretilecek sahne sayısı: {scriptData.Scenes.Count}.");

            // 2. AI İstemcisi
            var aiClient = await _aiFactory.ResolveImageClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);
            StoryboardStagePayload? storyboard = null;
            if (context.HasOutput(StageType.Storyboard))
            {
                storyboard = context.GetOutput<StoryboardStagePayload>(StageType.Storyboard);
                await logAsync($"Director Layer aktif. Mood: {PipelineLiveLog.Shorten(storyboard.VideoMood, 100)} Style: {PipelineLiveLog.Shorten(storyboard.StyleBible, 180)}");
            }
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);
            if (conceptProfile != null)
                await logAsync($"Gorsel konsept profili aktif. Stil: {PipelineLiveLog.Shorten(conceptProfile.VisualStyleName, 120)}");

            // 3. Kayıt Klasörü Hazırla
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "images");

   

            var results = new List<SceneImageItem>();
            var failures = new List<string>();
            int successCount = 0;

            // 4. Döngü (Sahneleri işle)
            foreach (var scene in scriptData.Scenes)
            {
                if (ct.IsCancellationRequested) break;

                var scenePlan = storyboard?.Scenes.FirstOrDefault(x => x.SceneNumber == scene.SceneNumber);
                var visualBeats = ImagePromptComposer.GetVisualBeats(scene, scenePlan, storyboard?.StyleBible, conceptProfile).ToList();
                await logAsync($"Sahne {scene.SceneNumber} için görsel üretimi başladı. Beat sayısı: {visualBeats.Count}.");

                foreach (var beat in visualBeats)
                {
                    var finalPrompt = ImagePromptComposer.BuildBeatPrompt(preset, scene, scenePlan, storyboard, beat, conceptProfile);
                    var quality = VisualVarietyEngine.ScorePrompt(scene, scenePlan, beat, finalPrompt);
                    await logAsync($"Sahne {scene.SceneNumber} / Beat {beat.BeatIndex} prompt hazırlandı: {PipelineLiveLog.Shorten(finalPrompt, 220)}");

                    try
                    {
                        // AI Çağrısı
                        var imageBytes = await AiImageRetryPolicy.GenerateImageAsync(
                            aiClient: aiClient,
                            operationLabel: $"Sahne {scene.SceneNumber} / Beat {beat.BeatIndex}",
                            prompt: finalPrompt,
                            negativePrompt: ImagePromptComposer.BuildNegativePrompt(preset, storyboard, beat, conceptProfile),
                            size: preset.Size,
                            style: preset.ArtStyle,
                            model: preset.ModelName,
                            logAsync: logAsync,
                            ct: ct
                        );

                        // Dosyayı Kaydet
                        var fileName = $"scene_{scene.SceneNumber:00}_b{beat.BeatIndex:00}_{Guid.NewGuid().ToString()[..6]}.png";
                        var fullPath = Path.Combine(outputDir, fileName);

                        await File.WriteAllBytesAsync(fullPath, imageBytes, ct);

                        results.Add(new SceneImageItem
                        {
                            SceneNumber = scene.SceneNumber,
                            BeatIndex = beat.BeatIndex,
                            BeatCount = visualBeats.Count,
                            BeatRole = beat.BeatRole,
                            VisualType = scene.VisualType,
                            VarietyRole = scene.VisualVarietyRole,
                            VarietyReason = scene.VisualVarietyReason,
                            ShotType = beat.ShotType,
                            EffectType = MapCameraMotionToEffect(beat.CameraMotion),
                            TransitionType = scenePlan?.TransitionType ?? scene.TransitionIntent ?? "cut",
                            OverlayText = !string.IsNullOrWhiteSpace(beat.OnScreenText)
                                ? beat.OnScreenText
                                : scenePlan?.OverlayText ?? scene.OverlayText ?? "",
                            DirectorIntent = !string.IsNullOrWhiteSpace(beat.CutIntent)
                                ? beat.CutIntent
                                : scenePlan?.ScenePurpose ?? scene.ScenePurpose ?? "",
                            ContinuityAnchor = scenePlan?.ContinuityAnchor ?? beat.ContinuityNotes,
                            Composition = beat.Composition,
                            Lens = beat.Lens,
                            Lighting = beat.Lighting,
                            ColorNotes = beat.ColorNotes,
                            CutIntent = beat.CutIntent,
                            VisualQualityScore = quality.Score,
                            VisualQualityNotes = quality.Notes,
                            ImagePath = fullPath,
                            PromptUsed = finalPrompt
                        });

                        successCount++;
                        // Başarılı log
                        await logAsync(PipelineLiveLog.Success($"Sahne {scene.SceneNumber} / Beat {beat.BeatIndex} görseli hazır. Kalite sinyali: {quality.Score}/100. Dosya: {fileName}."));
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = PipelineLiveLog.Error($"Sahne {scene.SceneNumber} / Beat {beat.BeatIndex} görsel üretimi başarısız oldu. Hata: {ex.Message}");

                        // Güvenlik filtresi uyarısı
                        if (ex.Message.Contains("safety") || ex.Message.Contains("content") || ex.Message.Contains("NO_IMAGE"))
                        {
                            errorMsg += " Olası sebep: prompt içindeki güvenlik filtresine takılan kelimeler.";
                        }

                        // Hata logunu canlıya bas
                        await logAsync(errorMsg);
                        failures.Add(errorMsg);

                        // Not: Burası catch bloğu olduğu için BaseExecutor zaten bu exception'ı yakalamayacak
                        // (çünkü biz burada yuttuk ve logladık). Eğer sahneyi atlayıp devam etmek istiyorsak
                        // 'throw' demeden devam ediyoruz. (Fallback mantığı için)
                    }
                }
            }

            if (successCount == 0)
            {
                var lastFailure = failures.LastOrDefault();
                var detail = string.IsNullOrWhiteSpace(lastFailure)
                    ? "Detay logu bulunamadı."
                    : lastFailure;

                throw new Exception($"Hiçbir görsel üretilemedi. Son hata: {detail}");
            }

            await logAsync(PipelineLiveLog.Success($"Görsel üretimi tamamlandı. Başarılı sahne: {successCount}/{scriptData.Scenes.Count}."));

            // 5. Sonuç Dön
            return new ImageStagePayload
            {
                ScriptId = scriptData.ScriptId,
                SceneImages = results
            };
        }

        private static IEnumerable<StoryboardVisualBeat> GetVisualBeats(
            ScriptSceneItem scene,
            StoryboardScenePlan? scenePlan,
            string? styleBible)
        {
            if (scenePlan?.VisualBeats != null && scenePlan.VisualBeats.Count > 0)
            {
                return scenePlan.VisualBeats
                    .OrderBy(x => x.BeatIndex)
                    .Take(4)
                    .Select((beat, index) =>
                    {
                        beat.BeatIndex = index + 1;
                        if (string.IsNullOrWhiteSpace(beat.VisualPrompt))
                            beat.VisualPrompt = scene.VisualPrompt;
                        return beat;
                    })
                    .ToList();
            }

            return new List<StoryboardVisualBeat>
            {
                new()
                {
                    BeatIndex = 1,
                    BeatRole = string.IsNullOrWhiteSpace(scene.VisualVarietyRole) ? "primary" : scene.VisualVarietyRole,
                    ShotType = DefaultShotType(scene.VisualType),
                    CameraMotion = MapCameraPlanToMotion(scene.CameraPlan),
                    Subject = "main narration idea",
                    Composition = DefaultComposition(scene.VisualType),
                    Lens = "35mm documentary lens",
                    Lighting = "motivated soft cinematic light",
                    ColorNotes = "match the video palette",
                    ContinuityNotes = string.IsNullOrWhiteSpace(scene.VisualVarietyReason)
                        ? "keep visual continuity with previous scenes"
                        : scene.VisualVarietyReason,
                    NegativePrompt = "text, watermark, logo, generic stock photo",
                    CutIntent = string.IsNullOrWhiteSpace(scene.ScenePurpose) ? "establish the scene idea" : scene.ScenePurpose,
                    VisualPrompt = string.IsNullOrWhiteSpace(styleBible)
                        ? scene.VisualPrompt
                        : $"{scene.VisualPrompt}, {styleBible}",
                    OnScreenText = scene.OverlayText,
                    DurationWeight = 1.0
                }
            };
        }

        private static string BuildBeatPrompt(
            ImagePreset preset,
            ScriptSceneItem scene,
            StoryboardScenePlan? scenePlan,
            StoryboardStagePayload? storyboard,
            StoryboardVisualBeat beat)
        {
            var artStyle = preset.ArtStyle ?? "cinematic";
            var beatPrompt = string.IsNullOrWhiteSpace(beat.VisualPrompt)
                ? scene.VisualPrompt
                : beat.VisualPrompt;

            var directorContext = string.Join(" ", new[]
            {
                $"Shot type: {beat.ShotType}. Camera motion feeling: {beat.CameraMotion}.",
                $"Subject: {beat.Subject}. Composition: {beat.Composition}. Lens: {beat.Lens}. Lighting: {beat.Lighting}.",
                $"Scene tone: {scenePlan?.EmotionalTone ?? scene.EmotionalBeat ?? "curious"}. Scene type: {scenePlan?.SceneType ?? scene.SceneRole ?? "explanation"}.",
                $"Scene purpose: {scenePlan?.ScenePurpose ?? scene.ScenePurpose ?? ""}. Retention goal: {scenePlan?.RetentionGoal ?? scene.ViewerQuestion ?? ""}.",
                $"Visual type: {scene.VisualType}. Camera plan: {scene.CameraPlan}.",
                $"Visual variety role: {scene.VisualVarietyRole}. Variety reason: {scene.VisualVarietyReason}.",
                $"Continuity anchor: {scenePlan?.ContinuityAnchor ?? beat.ContinuityNotes}.",
                $"Visual continuity: {storyboard?.VisualContinuityBible ?? storyboard?.StyleBible ?? artStyle}.",
                $"Color palette: {storyboard?.ColorPalette ?? beat.ColorNotes}. Camera language: {storyboard?.CameraLanguage ?? ""}.",
                $"Lighting style: {storyboard?.LightingStyle ?? beat.Lighting}. Beat color notes: {beat.ColorNotes}.",
                $"Avoid: {storyboard?.NegativeVisualRules ?? ""}, {beat.NegativePrompt}. No text, no captions, no watermark."
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var sceneDescription = $"{beatPrompt}. {directorContext}";

            var finalPrompt = ImagePromptDefaults.ResolvePromptTemplate(preset.PromptTemplate)
                .Replace("{SceneDescription}", sceneDescription)
                .Replace("{ArtStyle}", artStyle)
                .Trim();

            return string.IsNullOrWhiteSpace(finalPrompt)
                ? $"{sceneDescription}, {artStyle}"
                : finalPrompt;
        }

        private static string BuildNegativePrompt(
            ImagePreset preset,
            StoryboardStagePayload? storyboard,
            StoryboardVisualBeat beat)
        {
            var parts = new[]
            {
                preset.NegativePrompt,
                storyboard?.NegativeVisualRules,
                beat.NegativePrompt,
                "text, captions, watermark, logo, generic stock photo, distorted hands, distorted faces, low quality"
            };

            return string.Join(", ",
                parts
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string DefaultShotType(string visualType)
        {
            var token = NormalizeToken(visualType);
            return token switch
            {
                "timeline" => "timeline composition",
                "map" => "top-down map-like composition",
                "diagram" => "diagram-like composition",
                "quote_card" => "quote card background",
                "comparison" => "split comparison composition",
                "text_card" => "minimal text-card-safe background",
                "broll" or "b_roll" => "close-up b-roll detail",
                _ => "cinematic medium shot"
            };
        }

        private static string DefaultComposition(string visualType)
        {
            var token = NormalizeToken(visualType);
            return token switch
            {
                "timeline" => "clean timeline layout with clear visual hierarchy",
                "map" => "top-down map-like composition with strong focal area",
                "diagram" => "diagram-like composition with readable negative space",
                "quote_card" => "symbolic background with centered negative space",
                "comparison" => "split composition with contrasting visual zones",
                "text_card" => "minimal background with safe empty area for overlay",
                "broll" or "b_roll" => "off-center detail composition",
                _ => "clean subject-led cinematic composition"
            };
        }

        private static string MapCameraPlanToMotion(string? cameraPlan)
        {
            var token = NormalizeToken(cameraPlan);
            if (token.Contains("pull") || token.Contains("zoom_out"))
                return "slow_pull_out";
            if (token.Contains("pan_left"))
                return "pan_left";
            if (token.Contains("pan_right"))
                return "pan_right";
            if (token.Contains("static") || token.Contains("hold"))
                return "static_hold";
            return "slow_push_in";
        }

        private static string MapCameraMotionToEffect(string? cameraMotion)
        {
            var token = NormalizeToken(cameraMotion);
            return token switch
            {
                "slow_pull_out" or "pull_out" or "zoom_out" => "zoom_out",
                "pan_left" => "pan_left",
                "pan_right" => "pan_right",
                "pan_up" => "pan_up",
                "pan_down" => "pan_down",
                "static" or "static_hold" => "static",
                _ => "zoom_in"
            };
        }

        private static string NormalizeToken(string? value) =>
            (value ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }
}
