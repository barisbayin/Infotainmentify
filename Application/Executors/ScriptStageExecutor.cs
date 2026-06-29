using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Application.Services;
using Core.Attributes;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Executors
{
    [StageExecutor(StageType.Script)]
    [StagePreset(typeof(ScriptPreset))]
    public class ScriptStageExecutor : BaseStageExecutor
    {
        private static readonly JsonSerializerOptions RepairJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IRepository<Script> _scriptRepo;
        private readonly IUnitOfWork _uow;

        public ScriptStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IRepository<Script> scriptRepo,
            IUnitOfWork uow)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _scriptRepo = scriptRepo;
            _uow = uow;
        }

        public override StageType StageType => StageType.Script;

        public override async Task<object?> ProcessAsync(
                 ContentPipelineRun run,
                 StageConfig config,
                 StageExecution exec,
                 PipelineContext context,
                 object? presetObj,
                 Func<string, Task> logAsync,
                 CancellationToken ct)
        {
            var preset = (ScriptPreset)presetObj!;
            await logAsync($"Senaryo üretimi hazırlanıyor. Preset: {preset.Name}, model: {preset.ModelName}, hedef süre: {preset.TargetDurationSec} sn.");

            // 1. Topic Verisini Çek
            var topicPayload = context.GetOutput<TopicStagePayload>(StageType.Topic);
            if (topicPayload == null || string.IsNullOrEmpty(topicPayload.TopicText))
                throw new InvalidOperationException("Önceki adımdan (Topic) veri alınamadı.");

            await logAsync($"Kaynak konu alındı. Topic ID: {topicPayload.TopicId}, özet: '{PipelineLiveLog.Shorten(topicPayload.TopicText)}'.");
            var brief = topicPayload.Brief ?? ProductionBrief.FromJson(run.InputBriefJson);
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);
            if (brief != null)
                await logAsync($"Senaryo üretimi brief'e bağlandı. Ana başlık: {PipelineLiveLog.Shorten(brief.MainTitle, 140)}");
            if (conceptProfile != null)
                await logAsync($"Senaryo konsept profiline bağlandı. Ton: {PipelineLiveLog.Shorten(conceptProfile.Tone, 120)}, varsayılan süre: {conceptProfile.DefaultDurationSec?.ToString() ?? "yok"} sn.");
            var productionTarget = ProductionTarget.Resolve(brief, conceptProfile, preset.TargetDurationSec);
            var targetDurationSec = productionTarget.DurationSec;
            await logAsync(
                $"Senaryo hedef kontratı normalize edildi: {productionTarget.DurationSec} sn, " +
                $"kelime {productionTarget.MinNarrationWords}-{productionTarget.MaxNarrationWords}, " +
                $"sahne {productionTarget.MinSceneCount}-{productionTarget.MaxSceneCount}.");

            CreativeDirectorStagePayload? creativePlan = null;
            if (context.HasOutput(StageType.CreativeDirector))
            {
                creativePlan = context.GetOutput<CreativeDirectorStagePayload>(StageType.CreativeDirector);
                await logAsync($"Creative Director planı senaryoya bağlandı. Ana soru: {PipelineLiveLog.Shorten(creativePlan.CoreQuestion, 160)}");
            }

            // 2. AI İstemcisi
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // =================================================================
            // 🔥 REVİZE 1: PROMPT YAPISI (Array yerine Object istiyoruz)
            // =================================================================
            var systemPrompt = ScriptPromptDefaults.ResolveSystemInstruction(preset.SystemInstruction);

            systemPrompt += """

IMPORTANT OUTPUT CONTRACT:
Return ONLY a valid raw JSON object. No markdown, no code fences.
The JSON object must include:
{
  "title": "video title",
  "description": "SEO/video description",
  "tags": ["tag1", "tag2"],
  "scenes": [
    {
      "scene": 1,
      "sceneRole": "hook | setup | context | proof | example | reveal | recap | outro",
      "scenePurpose": "why this scene exists in the video",
      "viewerQuestion": "question the viewer should have during this scene",
      "emotionalBeat": "curiosity | tension | surprise | clarity | payoff",
      "visualType": "cinematic_image | broll | map | timeline | diagram | quote_card | comparison | text_card",
      "cameraPlan": "wide establishing shot, close-up detail, slow push-in, static hold, etc.",
      "overlayText": "short optional on-screen emphasis text, never more than 6 words",
      "sfxCue": "none | hit | whoosh | low_boom | silence",
      "transitionIntent": "cut | crossfade | dip_black | flash | match_cut",
      "chapterTitle": "short chapter/section title",
      "visualPrompt": "image-generation-ready scene visual, no text inside image",
      "audioText": "spoken narration for this scene",
      "durationSec": 8
    }
  ]
}
Scene rules:
- Every scene must serve the production brief and topic document.
- If a Creative Director plan is provided, follow its promise, core question, hook strategy, chapter plan and visual strategy.
- Do not drift away from the main title, angle, central question or avoid notes.
- For long-form videos, create a chapter-like progression with varied scene purposes.
- Keep audioText natural for TTS and avoid stage directions inside audioText.
- visualPrompt must describe what the viewer sees; do not ask the image to contain captions/text/logos.
- sceneRole, scenePurpose, viewerQuestion and emotionalBeat must explain the editing intent, not repeat the narration.
- visualType and cameraPlan must vary across consecutive scenes to prevent repetitive renders.
- overlayText and sfxCue are optional emphasis cues; keep them sparse and meaningful.
""";

            // Formatı netleştiriyoruz: Metadata + Scenes
//            systemPrompt += @"
//IMPORTANT: Output MUST be a valid JSON OBJECT with this exact structure:
//{
//  ""title"": ""Viral YouTube Shorts Title"",
//  ""description"": ""SEO optimized description with keywords"",
//  ""tags"": [""#tag1"", ""#tag2"", ""#tag3""],
//  ""scenes"": [
//    { ""scene"": 1, ""visual"": ""..."", ""audio"": ""..."", ""duration"": 5 }
//  ]
//}
//Do not use markdown blocks.";

            // 4. User Prompt
            var userPrompt = ScriptPromptDefaults.ResolvePromptTemplate(preset.PromptTemplate)
                .Replace("{Topic}", topicPayload.TopicText)
                .Replace("{MainTitle}", brief?.MainTitle ?? topicPayload.TopicTitle)
                .Replace("{BriefTitle}", brief?.MainTitle ?? topicPayload.TopicTitle)
                .Replace("{Angle}", brief?.Angle ?? topicPayload.Angle)
                .Replace("{Audience}", ProductionPromptContext.FirstNonEmpty(brief?.Audience, conceptProfile?.Audience))
                .Replace("{TargetDuration}", brief?.TargetDuration ?? targetDurationSec.ToString())
                .Replace("{MustCover}", brief?.MustCover ?? string.Join("\n", topicPayload.KeyPoints))
                .Replace("{Avoid}", brief?.Avoid ?? topicPayload.AvoidNotes)
                .Replace("{Notes}", brief?.Notes ?? "")
                .Replace("{Tone}", ProductionPromptContext.ResolveTone(conceptProfile, preset.Tone))
                .Replace("{Duration}", targetDurationSec.ToString())
                .Replace("{Language}", ProductionPromptContext.ResolveLanguage(conceptProfile, preset.Language));
            userPrompt = ProductionPromptContext.ApplyPlaceholders(userPrompt, conceptProfile);

            if (preset.IncludeHook) userPrompt += "\nRequirement: Start with a viral hook.";
            if (preset.IncludeCta) userPrompt += "\nRequirement: End with a call to action.";
            userPrompt += BuildSceneContractBlock(topicPayload, brief, preset, creativePlan, conceptProfile, productionTarget);

            // 5. AI Çağrısı
            await logAsync("AI'ya senaryo prompt'u gönderiliyor.");

            var responseJson = await aiClient.GenerateTextAsync(
                prompt: $"{systemPrompt}\n\n{userPrompt}",
                temperature: 0.7,
                model: preset.ModelName,
                ct: ct
            );

            await logAsync("AI yanıtı alındı. Senaryo JSON çıktısı ayrıştırılıyor.");

            // =================================================================
            // 🔥 REVİZE 2: JSON PARSE (Object -> Metadata + Scenes Array)
            // =================================================================
            var cleanJson = CleanJson(responseJson);
            ParsedScriptResult parsedScript;

            try
            {
                parsedScript = ParseScriptJson(cleanJson);
            }
            catch (Exception ex)
            {
                await logAsync(PipelineLiveLog.Error($"Senaryo JSON ayrıştırma hatası: {ex.Message}"));
                // Debug için ham veriyi loga basabiliriz (kısa halini)
                var preview = responseJson.Length > 200 ? responseJson.Substring(0, 200) + "..." : responseJson;
                throw new InvalidOperationException($"AI geçersiz JSON döndürdü: {ex.Message}. Raw: {preview}");
            }

            parsedScript = await RepairShortScriptIfNeededAsync(
                aiClient,
                parsedScript,
                systemPrompt,
                userPrompt,
                productionTarget,
                preset,
                logAsync,
                ct);

            var durationFixCount = NormalizeSceneDurationsFromNarration(parsedScript.Scenes, productionTarget);
            if (durationFixCount > 0)
                await logAsync(PipelineLiveLog.Info($"Sahne süreleri konuşma uzunluğuna göre normalize edildi. Güncellenen sahne: {durationFixCount}."));

            var scenes = parsedScript.Scenes;
            var varietySummary = VisualVarietyEngine.ApplyToScriptScenes(scenes);
            await logAsync(PipelineLiveLog.Info(
                $"Visual Variety V1 uygulandi. Degisen sahne: {varietySummary.ChangedScenes}, tekrar kirildi: {varietySummary.RepeatBreaks}, bilgi gorseli: {varietySummary.InfoVisualCount}. Dagilim: {varietySummary.DistributionText}."));

            var narrationWordCount = ValidateNarrationLengthOrThrow(scenes, productionTarget);
            await logAsync(PipelineLiveLog.Info(
                $"Senaryo kontrat kontrolü geçti. Kelime: {narrationWordCount}, sahne: {scenes.Count}, hedef süre: {targetDurationSec} sn."));

            var fullTextBuilder = new StringBuilder();
            foreach (var scene in scenes)
            {
                fullTextBuilder.AppendLine(scene.AudioText);
            }

            string aiTitle = parsedScript.Title;
            string aiDescription = parsedScript.Description;
            List<string> aiTags = parsedScript.Tags;

            // 7. DB'ye Kayıt
            // Eğer Script tablosunda Description/Tags sütunu yoksa şimdilik Content veya Title'a sığdırmayalım.
            // Ama Payload'a koyacağımız için sonraki aşama bunları kullanabilecek.

            // Eğer Title boş geldiyse Topic'i kullan
            if (string.IsNullOrEmpty(aiTitle)) aiTitle = topicPayload.TopicText;

            var scriptEntity = new Script
            {
                AppUserId = run.AppUserId,
                TopicId = topicPayload.TopicId,
                Title = aiTitle.Length > 250 ? aiTitle[..247] + "..." : aiTitle, // DB limitine dikkat
                Content = fullTextBuilder.ToString(),
                ScenesJson = JsonSerializer.Serialize(scenes),
                LanguageCode = preset.Language,
                EstimatedDurationSec = scenes.Sum(x => x.EstimatedDuration),
                SourcePresetId = preset.Id,
                CreatedByRunId = run.Id,
                CreatedAt = DateTime.UtcNow,
                Description = aiDescription,
                Tags = JsonSerializer.Serialize(aiTags)
            };

            await _scriptRepo.AddAsync(scriptEntity, ct);
            await _uow.SaveChangesAsync(ct);

            await logAsync(PipelineLiveLog.Success($"Senaryo kaydedildi. ID: {scriptEntity.Id}, sahne sayısı: {scenes.Count}, tahmini süre: {scriptEntity.EstimatedDurationSec} sn."));

            // =================================================================
            // 🔥 REVİZE 3: PAYLOAD'I DOLU DOLU DÖNMEK
            // =================================================================
            return new ScriptStagePayload
            {
                ScriptId = scriptEntity.Id,
                Title = aiTitle,
                FullScriptText = scriptEntity.Content,
                Scenes = scenes,

                // Upload aşaması için altın değerindeki veriler:
                Description = aiDescription,
                Tags = aiTags
            };
        }

        // --- HELPERS ---
        private static string BuildSceneContractBlock(
            TopicStagePayload topicPayload,
            ProductionBrief? brief,
            ScriptPreset preset,
            CreativeDirectorStagePayload? creativePlan,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile,
            ProductionTargetPlan target)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("SCENE GENERATION CONTRACT");
            sb.AppendLine($"Target language: {ProductionPromptContext.ResolveLanguage(conceptProfile, preset.Language)}");
            sb.AppendLine($"Tone: {ProductionPromptContext.ResolveTone(conceptProfile, preset.Tone)}");
            sb.AppendLine(target.ToPromptBlock());
            sb.AppendLine();

            if (brief != null)
            {
                sb.AppendLine(brief.ToPromptBlock());
                sb.AppendLine();
            }

            var conceptBlock = ProductionPromptContext.BuildScriptContextBlock(conceptProfile);
            if (!string.IsNullOrWhiteSpace(conceptBlock))
            {
                sb.AppendLine(conceptBlock);
                sb.AppendLine();
            }

            if (creativePlan != null)
            {
                sb.AppendLine(creativePlan.ToPromptBlock());
                sb.AppendLine();
            }

            sb.AppendLine("TOPIC DOCUMENT");
            sb.AppendLine(topicPayload.TopicText);

            if (!string.IsNullOrWhiteSpace(topicPayload.CentralQuestion))
                sb.AppendLine($"Central question: {topicPayload.CentralQuestion}");

            if (!string.IsNullOrWhiteSpace(topicPayload.Angle))
                sb.AppendLine($"Angle / thesis: {topicPayload.Angle}");

            if (topicPayload.KeyPoints.Count > 0)
            {
                sb.AppendLine("Must cover in scenes:");
                foreach (var item in topicPayload.KeyPoints)
                    sb.AppendLine($"- {item}");
            }

            if (topicPayload.ChapterHints.Count > 0)
            {
                sb.AppendLine("Chapter hints:");
                foreach (var item in topicPayload.ChapterHints)
                    sb.AppendLine($"- {item}");
            }

            if (!string.IsNullOrWhiteSpace(topicPayload.AvoidNotes))
                sb.AppendLine($"Avoid notes: {topicPayload.AvoidNotes}");

            sb.AppendLine("""

Long-form scene planning:
- Follow the Creative Director chapter plan when it is present.
- Build scenes as a progression: hook, context, escalation, proof/examples, reveal/payoff, recap/outro.
- Create enough scenes to support the target duration and scene count contract; do not compress a long video into a shorts structure.
- Do not inflate durationSec with very short one-sentence narration. The sum of audioText words must match the word budget above.
- For 8-12 minute videos, most audioText fields should be 20-35 spoken words. Chapter card scenes can be shorter, but they must be balanced by fuller narration scenes.
- durationSec must be realistic for the audioText length; assume around 150-170 spoken words per minute for TTS.
- If the production brief asks for a duration range such as 10-12 minutes, target the middle of that range and write enough spoken narration to actually fill it.
- If a moment needs more visual energy, keep the narration coherent and let Storyboard split that scene into multiple visual beats.
- Each scene needs a clear visualPrompt and spoken audioText.
- Vary scene purpose and visual angle so Storyboard/EditPlan can create non-monotone images and cuts later.
- For every scene, include sceneRole, scenePurpose, viewerQuestion, emotionalBeat, visualType, cameraPlan, transitionIntent, overlayText and sfxCue.
- visualType should rotate between cinematic_image, broll, timeline, map, diagram, comparison, quote_card and text_card when useful.
- cameraPlan should be concrete enough for Storyboard to choose shot type and motion.
""");

            return sb.ToString();
        }

        private async Task<ParsedScriptResult> RepairShortScriptIfNeededAsync(
            ITextGenerator aiClient,
            ParsedScriptResult parsedScript,
            string systemPrompt,
            string originalUserPrompt,
            ProductionTargetPlan target,
            ScriptPreset preset,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            if (!NeedsScriptRepair(parsedScript.Scenes, target, out var currentWords, out var repairReason))
                return parsedScript;

            await logAsync(PipelineLiveLog.Warning(
                $"Senaryo hedef kontrata göre kısa geldi ({repairReason}). Otomatik genişletme denemesi başlatılıyor."));

            var repairPrompt = BuildRepairPrompt(parsedScript, originalUserPrompt, target);

            try
            {
                var repairedResponse = await aiClient.GenerateTextAsync(
                    prompt: $"{systemPrompt}\n\n{repairPrompt}",
                    temperature: 0.55,
                    model: preset.ModelName,
                    ct: ct);

                var repaired = ParseScriptJson(CleanJson(repairedResponse));
                var repairedWords = repaired.Scenes.Sum(scene => CountWords(scene.AudioText));

                if (repairedWords > currentWords || repaired.Scenes.Count > parsedScript.Scenes.Count)
                {
                    await logAsync(PipelineLiveLog.Success(
                        $"Senaryo otomatik genişletildi. Kelime: {currentWords} -> {repairedWords}, sahne: {parsedScript.Scenes.Count} -> {repaired.Scenes.Count}."));
                    return repaired;
                }

                await logAsync(PipelineLiveLog.Warning(
                    $"Senaryo repair denemesi yeterli genişleme sağlamadı. Kelime: {repairedWords}, sahne: {repaired.Scenes.Count}. Mevcut doğrulama sonucu belirleyecek."));
            }
            catch (Exception ex)
            {
                await logAsync(PipelineLiveLog.Warning(
                    $"Senaryo repair denemesi başarısız oldu. Mevcut taslak doğrulamaya girecek. Hata: {PipelineLiveLog.Shorten(ex.Message, 240)}"));
            }

            return parsedScript;
        }

        private static bool NeedsScriptRepair(
            List<ScriptSceneItem> scenes,
            ProductionTargetPlan target,
            out int totalWords,
            out string reason)
        {
            totalWords = scenes.Sum(scene => CountWords(scene.AudioText));
            reason = "";

            if (!target.IsLongForm)
                return false;

            var issues = new List<string>();
            if (totalWords < target.MinNarrationWords)
                issues.Add($"kelime {totalWords}/{target.MinNarrationWords}");

            if (scenes.Count < target.MinSceneCount)
                issues.Add($"sahne {scenes.Count}/{target.MinSceneCount}");

            reason = string.Join(", ", issues);
            return issues.Count > 0;
        }

        private static string BuildRepairPrompt(
            ParsedScriptResult parsedScript,
            string originalUserPrompt,
            ProductionTargetPlan target)
        {
            var currentJson = JsonSerializer.Serialize(new
            {
                title = parsedScript.Title,
                description = parsedScript.Description,
                tags = parsedScript.Tags,
                scenes = parsedScript.Scenes.Select(scene => new
                {
                    scene = scene.SceneNumber,
                    sceneRole = scene.SceneRole,
                    scenePurpose = scene.ScenePurpose,
                    viewerQuestion = scene.ViewerQuestion,
                    emotionalBeat = scene.EmotionalBeat,
                    visualType = scene.VisualType,
                    cameraPlan = scene.CameraPlan,
                    overlayText = scene.OverlayText,
                    sfxCue = scene.SfxCue,
                    transitionIntent = scene.TransitionIntent,
                    chapterTitle = scene.ChapterTitle,
                    visualPrompt = scene.VisualPrompt,
                    audioText = scene.AudioText,
                    durationSec = scene.EstimatedDuration
                })
            }, RepairJsonOptions);

            return $$"""
The first script draft is too short for the production target. Expand it into a complete replacement JSON object.

{{target.ToPromptBlock()}}

Repair rules:
- Return ONLY the full valid JSON object. No markdown, no comments.
- Preserve the title, central idea, brief constraints, chapter progression and concept style.
- Expand narration naturally with concrete explanation, examples, setups, payoffs and transitions.
- Add scenes when needed; aim near the ideal scene count, not just the minimum.
- Most long-form scenes should contain 20-35 spoken words and last 8-14 seconds.
- Do not pad with filler, repeated sentences or disconnected captions.
- Keep every scene self-contained for image generation and TTS.
- Keep the same JSON field names required by the output contract.

ORIGINAL PROMPT CONTEXT:
{{LimitForPrompt(originalUserPrompt, 12000)}}

CURRENT SHORT JSON:
{{LimitForPrompt(currentJson, 26000)}}
""";
        }

        private static int NormalizeSceneDurationsFromNarration(List<ScriptSceneItem> scenes, ProductionTargetPlan target)
        {
            var changed = 0;
            foreach (var scene in scenes)
            {
                var words = CountWords(scene.AudioText);
                if (words <= 0) continue;

                var estimatedSpeechSeconds = (int)Math.Round(words / 2.55);
                var minDuration = target.IsLongForm ? 5 : 2;
                var maxDuration = target.IsLongForm ? 18 : 10;
                var normalized = Math.Clamp(estimatedSpeechSeconds, minDuration, maxDuration);

                if ((scene.EstimatedDuration <= 5 && words >= 18)
                    || Math.Abs(scene.EstimatedDuration - normalized) >= 3)
                {
                    scene.EstimatedDuration = normalized;
                    changed++;
                }
            }

            return changed;
        }

        private static string LimitForPrompt(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value[..Math.Max(0, maxLength - 80)] + "\n...[truncated for repair prompt]...";
        }

        private static int ResolveTargetDurationSeconds(string? rawTargetDuration, int fallbackSeconds)
        {
            var fallback = fallbackSeconds > 0 ? fallbackSeconds : 60;
            if (string.IsNullOrWhiteSpace(rawTargetDuration)) return fallback;

            var normalized = rawTargetDuration.Trim().ToLowerInvariant();
            var numberMatches = Regex.Matches(normalized, @"\d+(?:[\.,]\d+)?");
            if (numberMatches.Count == 0) return fallback;

            var numbers = numberMatches
                .Select(match => match.Value.Replace(',', '.'))
                .Select(value => double.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
                .Where(value => value > 0)
                .ToList();

            if (numbers.Count == 0) return fallback;

            var selected = numbers.Count >= 2
                ? (numbers[0] + numbers[1]) / 2.0
                : numbers[0];

            var hasMinuteUnit = ContainsAny(normalized, "minute", "minutes", "min", "dk", "dakika");
            var hasHourUnit = ContainsAny(normalized, "hour", "hours", "saat");
            var hasSecondUnit = ContainsAny(normalized, "second", "seconds", "sec", "sn", "saniye");

            double seconds;
            if (hasHourUnit)
            {
                seconds = selected * 3600;
            }
            else if (hasMinuteUnit)
            {
                seconds = selected * 60;
            }
            else if (hasSecondUnit)
            {
                seconds = selected;
            }
            else
            {
                seconds = selected >= 240 ? selected : fallback;
            }

            return (int)Math.Round(Math.Clamp(seconds, 15, 3600));
        }

        private static int ValidateNarrationLengthOrThrow(List<ScriptSceneItem> scenes, ProductionTargetPlan target)
        {
            var totalWords = scenes.Sum(scene => CountWords(scene.AudioText));
            if (!target.IsLongForm) return totalWords;

            if (totalWords < target.MinNarrationWords || scenes.Count < target.MinSceneCount)
            {
                throw new InvalidOperationException(
                    $"Senaryo hedef kontrata göre kısa kaldı. Hedef: {target.DurationSec} sn, " +
                    $"minimum kelime: {target.MinNarrationWords}, gelen kelime: {totalWords}, " +
                    $"minimum sahne: {target.MinSceneCount}, gelen sahne: {scenes.Count}. " +
                    "AI durationSec alanlarını doldurmuş olabilir ama audioText ve sahne yoğunluğu gerçek long-form üretimi taşıyacak kadar güçlü değil.");
            }

            return totalWords;
        }

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return Regex.Matches(text, @"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)?").Count;
        }

        private static int EstimateMinNarrationWords(int targetDurationSec)
            => (int)Math.Round(targetDurationSec * 2.35);

        private static int EstimateIdealNarrationWords(int targetDurationSec)
            => (int)Math.Round(targetDurationSec * 2.65);

        private static int EstimateMaxNarrationWords(int targetDurationSec)
            => (int)Math.Round(targetDurationSec * 3.05);

        private static bool ContainsAny(string value, params string[] needles)
            => needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

        private string CleanJson(string text)
        {
            text = text.Trim();

            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewLine = text.IndexOf('\n');
                if (firstNewLine >= 0) text = text[(firstNewLine + 1)..];
                if (text.EndsWith("```", StringComparison.Ordinal)) text = text[..^3];
            }

            var firstJsonChar = text.IndexOfAny(new[] { '{', '[' });
            if (firstJsonChar > 0) text = text[firstJsonChar..];

            var lastObjectEnd = text.LastIndexOf('}');
            var lastArrayEnd = text.LastIndexOf(']');
            var lastJsonChar = Math.Max(lastObjectEnd, lastArrayEnd);
            if (lastJsonChar >= 0 && lastJsonChar < text.Length - 1)
                text = text[..(lastJsonChar + 1)];

            return text.Trim();
        }

        private static ParsedScriptResult ParseScriptJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new ParsedScriptResult();
            JsonElement scenesElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                scenesElement = root;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                result.Title = GetStr(root, "title", "videoTitle", "name");
                result.Description = GetStr(root, "description", "videoDescription", "summary");
                result.Tags = GetStringList(root, "tags", "hashtags", "keywords");

                if (TryGetProperty(root, out scenesElement, "scenes", "segments", "items"))
                {
                    if (scenesElement.ValueKind != JsonValueKind.Array)
                        throw new InvalidOperationException("'scenes' alanı array olmalı.");
                }
                else if (LooksLikeScene(root))
                {
                    var singleScene = ReadScene(root, 1);
                    if (singleScene != null) result.Scenes.Add(singleScene);
                    return EnsureValidParsedScript(result);
                }
                else
                {
                    throw new InvalidOperationException("JSON object içinde 'scenes' dizisi bulunamadı.");
                }
            }
            else
            {
                throw new InvalidOperationException("JSON kökü object veya array olmalı.");
            }

            var ordinal = 1;
            foreach (var element in scenesElement.EnumerateArray())
            {
                var scene = ReadScene(element, ordinal);
                if (scene != null)
                {
                    result.Scenes.Add(scene);
                    ordinal++;
                }
            }

            return EnsureValidParsedScript(result);
        }

        private static ParsedScriptResult EnsureValidParsedScript(ParsedScriptResult result)
        {
            if (!result.Scenes.Any())
                throw new InvalidOperationException("JSON içinde okunabilir sahne bulunamadı.");

            return result;
        }

        private static ScriptSceneItem? ReadScene(JsonElement element, int fallbackSceneNumber)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var text = element.GetString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(text)) return null;

                return new ScriptSceneItem
                {
                    SceneNumber = fallbackSceneNumber,
                    VisualPrompt = text,
                    AudioText = text,
                    EstimatedDuration = 5,
                    SceneRole = fallbackSceneNumber == 1 ? "hook" : "explanation",
                    ScenePurpose = fallbackSceneNumber == 1 ? "Open the video and create curiosity." : "Advance the narration.",
                    ViewerQuestion = fallbackSceneNumber == 1 ? "Why should I keep watching?" : "",
                    EmotionalBeat = fallbackSceneNumber == 1 ? "curiosity" : "clarity",
                    VisualType = "cinematic_image",
                    CameraPlan = "cinematic medium shot with slow push-in",
                    SfxCue = fallbackSceneNumber == 1 ? "low_boom" : "none",
                    TransitionIntent = "cut"
                };
            }

            if (element.ValueKind != JsonValueKind.Object)
                return null;

            var audio = GetStr(element, "audio", "audioText", "voiceover", "voiceOver", "narration", "text", "script", "dialogue", "line");
            var visual = GetStr(element, "visual", "visualPrompt", "imagePrompt", "sceneDescription", "description", "image", "prompt");

            if (string.IsNullOrWhiteSpace(audio) && string.IsNullOrWhiteSpace(visual))
                return null;

            if (string.IsNullOrWhiteSpace(visual)) visual = audio;

            var sceneNumber = GetInt(element, fallbackSceneNumber, "scene", "sceneNumber", "number", "index", "id");
            if (sceneNumber <= 0) sceneNumber = fallbackSceneNumber;

            var duration = GetInt(element, 5, "duration", "durationSec", "estimatedDuration", "estimatedDurationSec", "seconds");
            if (duration <= 0) duration = 5;

            var sceneRole = NormalizeToken(
                GetStr(element, "sceneRole", "role", "sceneType", "type"),
                sceneNumber == 1 ? "hook" : "explanation");
            var scenePurpose = GetStr(element, "scenePurpose", "purpose", "intent", "directorIntent");
            var viewerQuestion = GetStr(element, "viewerQuestion", "question", "retentionQuestion");
            var emotionalBeat = NormalizeToken(
                GetStr(element, "emotionalBeat", "emotionalTone", "emotion", "tone", "mood"),
                sceneNumber == 1 ? "curiosity" : "clarity");
            var visualType = NormalizeVisualType(GetStr(element, "visualType", "visualFormat", "format"));
            var cameraPlan = GetStr(element, "cameraPlan", "camera", "shotPlan", "shot", "framing");
            var overlayText = GetStr(element, "overlayText", "onScreenText", "textOverlay");
            var sfxCue = NormalizeToken(GetStr(element, "sfxCue", "soundCue", "audioCue", "sfx"), "none");
            var transitionIntent = NormalizeTransition(GetStr(element, "transitionIntent", "transitionType", "transition"));
            var chapterTitle = GetStr(element, "chapterTitle", "chapter", "sectionTitle", "section");

            return new ScriptSceneItem
            {
                SceneNumber = sceneNumber,
                VisualPrompt = visual,
                AudioText = audio,
                EstimatedDuration = duration,
                SceneRole = sceneRole,
                ScenePurpose = string.IsNullOrWhiteSpace(scenePurpose)
                    ? (sceneNumber == 1 ? "Create curiosity and establish the video promise." : "Move the viewer to the next idea.")
                    : scenePurpose,
                ViewerQuestion = viewerQuestion,
                EmotionalBeat = emotionalBeat,
                VisualType = visualType,
                CameraPlan = string.IsNullOrWhiteSpace(cameraPlan)
                    ? DefaultCameraPlan(visualType, sceneNumber)
                    : cameraPlan,
                OverlayText = overlayText,
                SfxCue = sfxCue,
                TransitionIntent = transitionIntent,
                ChapterTitle = chapterTitle
            };
        }

        private static bool LooksLikeScene(JsonElement element)
            => GetStr(element, "audio", "audioText", "voiceover", "voiceOver", "narration", "text", "script", "dialogue", "line") != ""
               || GetStr(element, "visual", "visualPrompt", "imagePrompt", "sceneDescription", "description", "image", "prompt") != "";

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

        private static List<string> GetStringList(JsonElement el, params string[] props)
        {
            if (!TryGetProperty(el, out var p, props)) return new List<string>();

            if (p.ValueKind == JsonValueKind.Array)
            {
                return p.EnumerateArray()
                    .Select(ToPlainString)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var raw = ToPlainString(p);
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

            var separators = raw.Contains(',') || raw.Contains(';') || raw.Contains('\n')
                ? new[] { ',', ';', '\n', '\r' }
                : new[] { ' ' };

            return raw.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ToPlainString(JsonElement el)
            => el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.GetRawText();

        private static int GetInt(JsonElement el, int fallback, params string[] props)
        {
            if (!TryGetProperty(el, out var p, props)) return fallback;

            if (p.ValueKind == JsonValueKind.Number)
            {
                if (p.TryGetInt32(out var i)) return i;
                if (p.TryGetDouble(out var d)) return (int)Math.Round(d);
            }

            if (p.ValueKind == JsonValueKind.String
                && double.TryParse(p.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return (int)Math.Round(parsed);
            }

            return fallback;
        }

        private static string NormalizeVisualType(string value)
        {
            var token = NormalizeToken(value, "cinematic_image");
            return token switch
            {
                "image" or "cinematic" or "cinematic_image" => "cinematic_image",
                "b_roll" or "broll" => "broll",
                "map" => "map",
                "timeline" => "timeline",
                "diagram" => "diagram",
                "quote" or "quote_card" => "quote_card",
                "comparison" or "comparison_card" => "comparison",
                "text" or "text_card" => "text_card",
                _ => "cinematic_image"
            };
        }

        private static string NormalizeTransition(string value)
        {
            var token = NormalizeToken(value, "cut");
            return token switch
            {
                "crossfade" or "fade" => "crossfade",
                "dip_black" or "dip_to_black" => "dip_black",
                "flash" or "flash_white" => "flash",
                "match_cut" => "match_cut",
                _ => "cut"
            };
        }

        private static string DefaultCameraPlan(string visualType, int sceneNumber)
        {
            return visualType switch
            {
                "timeline" => "clean timeline composition, static hold, readable visual hierarchy",
                "map" => "top-down map-like composition, slow push-in toward the key region",
                "diagram" => "diagram-like composition with strong negative space, static hold",
                "quote_card" => "close-up symbolic background with centered negative space for overlay",
                "comparison" => "split composition with two contrasting visual zones",
                "broll" => "detail shot with shallow depth of field and slow lateral motion",
                _ => sceneNumber == 1
                    ? "wide cinematic establishing shot with slow push-in"
                    : "medium cinematic shot with motivated camera movement"
            };
        }

        private static string NormalizeToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        private sealed class ParsedScriptResult
        {
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public List<string> Tags { get; set; } = new();
            public List<ScriptSceneItem> Scenes { get; set; } = new();
        }
    }
}
