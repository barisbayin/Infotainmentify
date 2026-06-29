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
    [StageExecutor(StageType.CreativeDirector)]
    public class CreativeDirectorStageExecutor : BaseStageExecutor
    {
        private const int MaxChapters = 8;

        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IRepository<ScriptPreset> _scriptPresetRepo;

        public CreativeDirectorStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IRepository<ScriptPreset> scriptPresetRepo)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _scriptPresetRepo = scriptPresetRepo;
        }

        public override StageType StageType => StageType.CreativeDirector;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            var topic = context.GetOutput<TopicStagePayload>(StageType.Topic);
            if (topic == null || string.IsNullOrWhiteSpace(topic.TopicText))
                throw new InvalidOperationException("Creative Director icin Topic ciktisi bulunamadi.");

            var brief = topic.Brief ?? ProductionBrief.FromJson(run.InputBriefJson);
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);
            await logAsync($"Creative Director hazirlaniyor. Kaynak konu: {PipelineLiveLog.Shorten(topic.TopicTitle, 140)}.");
            if (conceptProfile != null)
                await logAsync($"Creative Director konsept profilini kullanacak. Vaad: {PipelineLiveLog.Shorten(conceptProfile.ChannelPromise, 160)}");

            var scriptPreset = await TryResolveScriptPresetAsync(run, ct);
            if (scriptPreset == null)
            {
                await logAsync(PipelineLiveLog.Warning("Script preset bulunamadi. Creative Director fallback plan uretecek."));
                return BuildFallbackPlan(topic, brief, null, conceptProfile);
            }

            try
            {
                var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, scriptPreset.UserAiConnectionId, ct);
                var prompt = BuildPrompt(topic, brief, scriptPreset, conceptProfile);

                await logAsync($"AI'ya Creative Director istegi gonderiliyor. Model: {scriptPreset.ModelName}.");
                var response = await aiClient.GenerateTextAsync(
                    prompt,
                    temperature: 0.68,
                    model: scriptPreset.ModelName,
                    ct: ct);

                var plan = ParsePlan(response, topic, brief, scriptPreset, conceptProfile);
                await logAsync(PipelineLiveLog.Success($"Creative Director plani uretildi. Bolum: {plan.Chapters.Count}, vaad: {PipelineLiveLog.Shorten(plan.VideoPromise, 180)}"));
                return plan;
            }
            catch (Exception ex)
            {
                await logAsync(PipelineLiveLog.Warning($"Creative Director AI uretimi basarisiz oldu. Fallback plan kullanilacak. Hata: {PipelineLiveLog.Shorten(ex.Message, 280)}"));
                return BuildFallbackPlan(topic, brief, scriptPreset, conceptProfile);
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

        private static string BuildPrompt(
            TopicStagePayload topic,
            ProductionBrief? brief,
            ScriptPreset preset,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var target = ProductionTarget.Resolve(brief, conceptProfile, preset.TargetDurationSec);

            return $$"""
You are the creative director, retention strategist and YouTube long-form producer for this automated video workflow.
Create the high-level production plan that the scriptwriter, storyboard artist and editor will follow.

Return ONLY raw JSON. No markdown, no code fences.

JSON shape:
{
  "directorVersion": "v1",
  "videoPromise": "what the video promises the viewer",
  "coreQuestion": "main question that keeps the viewer watching",
  "viewerProfile": "who this is for",
  "narrativeAngle": "specific thesis or perspective",
  "tone": "documentary | mystery | energetic | calm | academic | cinematic",
  "hookStrategy": "how the first 30 seconds earns attention",
  "retentionStrategy": "how curiosity is renewed across the video",
  "visualStrategy": "overall visual approach and variety rules",
  "pacingStrategy": "how fast/slow chapters should feel",
  "emotionalArc": "viewer feeling from start to finish",
  "payoff": "what the ending should deliver",
  "avoidNotes": "things to avoid",
  "mustCover": ["specific points that must appear"],
  "visualFormats": ["cinematic image", "b-roll", "timeline", "map", "diagram", "quote card", "comparison"],
  "chapters": [
    {
      "chapterIndex": 1,
      "title": "short chapter title",
      "purpose": "why this chapter exists",
      "viewerQuestion": "question in viewer's mind during this chapter",
      "emotionalBeat": "curiosity | tension | surprise | clarity | payoff",
      "visualDirection": "what kind of visuals should dominate",
      "pacing": "fast | balanced | slow | impact",
      "keyPoints": ["chapter point"]
    }
  ]
}

Rules:
- Build 4 to {{MaxChapters}} chapters for long-form unless the target duration is very short.
- The plan must not write the full script. It gives direction to the next stages.
- Make every chapter answer or deepen the core question.
- Include visual variety guidance so image generation avoids repetition.
- Keep the plan faithful to the user's brief, topic document and avoid notes.

Production target:
Language/culture: {{ProductionPromptContext.ResolveLanguage(conceptProfile, preset.Language)}}
Tone: {{ProductionPromptContext.ResolveTone(conceptProfile, preset.Tone)}}
{{target.ToPromptBlock()}}

{{(brief == null ? "" : brief.ToPromptBlock())}}

{{ProductionPromptContext.BuildCreativeDirectorContextBlock(conceptProfile)}}

TOPIC DOCUMENT
Title: {{topic.TopicTitle}}
{{topic.TopicText}}

Topic signals:
Audience promise: {{topic.AudiencePromise}}
Central question: {{topic.CentralQuestion}}
Angle: {{topic.Angle}}
Avoid notes: {{topic.AvoidNotes}}
""";
        }

        private static CreativeDirectorStagePayload ParsePlan(
            string response,
            TopicStagePayload topic,
            ProductionBrief? brief,
            ScriptPreset preset,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var cleanJson = CleanJson(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Creative Director JSON koku object olmali.");

            var plan = new CreativeDirectorStagePayload
            {
                DirectorVersion = NormalizeText(GetStr(root, "directorVersion", "version"), "v1"),
                VideoPromise = GetStr(root, "videoPromise", "promise", "audiencePromise"),
                CoreQuestion = GetStr(root, "coreQuestion", "centralQuestion", "question"),
                ViewerProfile = GetStr(root, "viewerProfile", "audience", "targetAudience"),
                NarrativeAngle = GetStr(root, "narrativeAngle", "angle", "thesis"),
                Tone = NormalizeText(GetStr(root, "tone", "videoTone"), preset.Tone),
                HookStrategy = GetStr(root, "hookStrategy", "hook"),
                RetentionStrategy = GetStr(root, "retentionStrategy", "retention"),
                VisualStrategy = GetStr(root, "visualStrategy", "visualDirection", "visualPlan"),
                PacingStrategy = GetStr(root, "pacingStrategy", "pacing"),
                EmotionalArc = GetStr(root, "emotionalArc", "arc"),
                Payoff = GetStr(root, "payoff", "ending", "finalPayoff"),
                AvoidNotes = GetStr(root, "avoidNotes", "avoid", "negativeNotes"),
                MustCover = GetStringList(root, "mustCover", "keyPoints", "mustInclude"),
                VisualFormats = GetStringList(root, "visualFormats", "visualTypes", "formats")
            };

            if (TryGetProperty(root, out var chaptersElement, "chapters", "sections", "chapterPlan")
                && chaptersElement.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var chapterElement in chaptersElement.EnumerateArray().Take(MaxChapters))
                {
                    if (chapterElement.ValueKind != JsonValueKind.Object) continue;

                    plan.Chapters.Add(new CreativeDirectorChapterPlan
                    {
                        ChapterIndex = Math.Max(1, GetInt(chapterElement, index, "chapterIndex", "index", "number")),
                        Title = NormalizeText(GetStr(chapterElement, "title", "name"), $"Chapter {index}"),
                        Purpose = GetStr(chapterElement, "purpose", "intent"),
                        ViewerQuestion = GetStr(chapterElement, "viewerQuestion", "question"),
                        EmotionalBeat = GetStr(chapterElement, "emotionalBeat", "beat", "emotion"),
                        VisualDirection = GetStr(chapterElement, "visualDirection", "visual", "visualPlan"),
                        Pacing = NormalizePacing(GetStr(chapterElement, "pacing", "tempo")),
                        KeyPoints = GetStringList(chapterElement, "keyPoints", "points", "mustCover")
                    });
                    index++;
                }
            }

            NormalizePlan(plan, topic, brief, preset, conceptProfile);
            return plan;
        }

        private static CreativeDirectorStagePayload BuildFallbackPlan(
            TopicStagePayload topic,
            ProductionBrief? brief,
            ScriptPreset? preset,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var chapters = BuildFallbackChapters(topic);
            var mustCover = topic.KeyPoints.Count > 0
                ? topic.KeyPoints
                : SplitLines(brief?.MustCover);

            var plan = new CreativeDirectorStagePayload
            {
                DirectorVersion = "v1-fallback",
                VideoPromise = FirstNonEmpty(topic.AudiencePromise, brief?.Angle, topic.TopicTitle),
                CoreQuestion = FirstNonEmpty(topic.CentralQuestion, $"Why does '{topic.TopicTitle}' matter?"),
                ViewerProfile = FirstNonEmpty(brief?.Audience, conceptProfile?.Audience, "curious YouTube viewer"),
                NarrativeAngle = FirstNonEmpty(topic.Angle, brief?.Angle, topic.TopicTitle),
                Tone = FirstNonEmpty(conceptProfile?.Tone, preset?.Tone, "cinematic documentary"),
                HookStrategy = "Open with the most surprising tension in the topic, then promise a clear payoff.",
                RetentionStrategy = "Renew curiosity every chapter with a fresh question, example or contrast.",
                VisualStrategy = FirstNonEmpty(
                    conceptProfile?.VisualStyleBible,
                    "Mix cinematic images, symbolic b-roll, timelines, comparison cards and detail shots. Avoid repeating the same composition twice in a row."),
                PacingStrategy = "Start fast, settle into balanced explanation, then slow down for key reveals and finish with impact.",
                EmotionalArc = "curiosity -> tension -> clarity -> payoff",
                Payoff = "End by answering the core question and leaving the viewer with one memorable takeaway.",
                AvoidNotes = FirstNonEmpty(topic.AvoidNotes, brief?.Avoid),
                MustCover = mustCover,
                VisualFormats = new List<string> { "cinematic image", "b-roll", "timeline", "diagram", "comparison card", "quote card" },
                Chapters = chapters
            };

            NormalizePlan(plan, topic, brief, preset, conceptProfile);
            return plan;
        }

        private static List<CreativeDirectorChapterPlan> BuildFallbackChapters(TopicStagePayload topic)
        {
            var hints = topic.ChapterHints
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Take(MaxChapters)
                .ToList();

            if (hints.Count == 0)
            {
                hints = new List<string>
                {
                    "Hook",
                    "Context",
                    "Main tension",
                    "Proof and examples",
                    "Reveal",
                    "Payoff"
                };
            }

            var result = new List<CreativeDirectorChapterPlan>();
            for (var i = 0; i < hints.Count; i++)
            {
                result.Add(new CreativeDirectorChapterPlan
                {
                    ChapterIndex = i + 1,
                    Title = hints[i],
                    Purpose = i == 0
                        ? "Earn attention and frame the promise."
                        : i == hints.Count - 1
                            ? "Deliver the payoff and close the loop."
                            : "Advance the central question with a new layer.",
                    ViewerQuestion = i == 0
                        ? "Why should I care?"
                        : "What does this change about the main idea?",
                    EmotionalBeat = i == 0 ? "curiosity" : i == hints.Count - 1 ? "payoff" : "tension",
                    VisualDirection = i % 3 == 0
                        ? "cinematic establishing visuals and strong symbols"
                        : i % 3 == 1
                            ? "evidence, timelines and comparison visuals"
                            : "close details, reveal shots and b-roll contrast",
                    Pacing = i == 0 ? "fast" : i == hints.Count - 1 ? "impact" : "balanced",
                    KeyPoints = topic.KeyPoints.Skip(i).Take(2).ToList()
                });
            }

            return result;
        }

        private static void NormalizePlan(
            CreativeDirectorStagePayload plan,
            TopicStagePayload topic,
            ProductionBrief? brief,
            ScriptPreset? preset,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            plan.DirectorVersion = NormalizeText(plan.DirectorVersion, "v1");
            plan.VideoPromise = NormalizeText(plan.VideoPromise, FirstNonEmpty(topic.AudiencePromise, topic.TopicTitle));
            plan.CoreQuestion = NormalizeText(plan.CoreQuestion, FirstNonEmpty(topic.CentralQuestion, $"What is the real story behind {topic.TopicTitle}?"));
            plan.ViewerProfile = NormalizeText(plan.ViewerProfile, FirstNonEmpty(brief?.Audience, conceptProfile?.Audience, "curious YouTube viewer"));
            plan.NarrativeAngle = NormalizeText(plan.NarrativeAngle, FirstNonEmpty(topic.Angle, brief?.Angle, topic.TopicTitle));
            plan.Tone = NormalizeText(plan.Tone, FirstNonEmpty(conceptProfile?.Tone, preset?.Tone, "cinematic documentary"));
            plan.HookStrategy = NormalizeText(plan.HookStrategy, "Open with a sharp question, surprising contrast or visual mystery.");
            plan.RetentionStrategy = NormalizeText(plan.RetentionStrategy, "Use chapter-level questions, visual refreshes and reveals to renew attention.");
            plan.VisualStrategy = NormalizeText(plan.VisualStrategy, FirstNonEmpty(conceptProfile?.VisualStyleBible, "Use varied shot scales, b-roll, timeline/card visuals and recurring motifs."));
            plan.PacingStrategy = NormalizeText(plan.PacingStrategy, "Alternate fast setup, balanced explanation and slower key reveals.");
            plan.EmotionalArc = NormalizeText(plan.EmotionalArc, "curiosity -> tension -> clarity -> payoff");
            plan.Payoff = NormalizeText(plan.Payoff, "Answer the core question with a memorable final takeaway.");
            plan.AvoidNotes = NormalizeText(plan.AvoidNotes, FirstNonEmpty(topic.AvoidNotes, brief?.Avoid));

            if (plan.MustCover.Count == 0)
                plan.MustCover = topic.KeyPoints.Count > 0 ? topic.KeyPoints : SplitLines(brief?.MustCover);

            if (plan.VisualFormats.Count == 0)
                plan.VisualFormats = new List<string> { "cinematic image", "b-roll", "timeline", "diagram", "comparison card" };

            if (plan.Chapters.Count == 0)
                plan.Chapters = BuildFallbackChapters(topic);
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

            return SplitLines(ToPlainString(p));
        }

        private static string ToPlainString(JsonElement el)
            => el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.GetRawText();

        private static int GetInt(JsonElement el, int fallback, params string[] props)
        {
            if (!TryGetProperty(el, out var p, props)) return fallback;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i)) return i;
            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var parsed)) return parsed;
            return fallback;
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

        private static string NormalizeToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        private static string NormalizeText(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";

        private static List<string> SplitLines(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();

            return value
                .Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim('-', ' ', '\t'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
