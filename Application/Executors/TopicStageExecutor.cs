using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Extensions;
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

namespace Application.Executors
{
    [StageExecutor(StageType.Topic)]
    [StagePreset(typeof(TopicPreset))]
    public class TopicStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IRepository<Topic> _topicRepo;
        private readonly IUnitOfWork _uow;

        public TopicStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IRepository<Topic> topicRepo,
            IUnitOfWork uow)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _topicRepo = topicRepo;
            _uow = uow;
        }

        public override StageType StageType => StageType.Topic;

        // 🔥 FIX 1: Changed to 'protected override' and added 'logAsync'
        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync, // <--- Live Logging Function
            CancellationToken ct)
        {
            var preset = (TopicPreset)presetObj!;
            var brief = ProductionBrief.FromJson(run.InputBriefJson);
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);
            var productionTarget = ProductionTarget.Resolve(
                brief,
                conceptProfile,
                conceptProfile?.DefaultDurationSec ?? 600);

            // 🔥 FIX 2: Using logAsync instead of exec.AddLog
            await logAsync($"Konu üretimi hazırlanıyor. Model: {preset.ModelName}, dil: {preset.Language}.");
            if (brief != null)
                await logAsync($"Üretim brief'i aktif. Ana başlık: {PipelineLiveLog.Shorten(brief.MainTitle, 140)}");
            if (conceptProfile != null)
                await logAsync($"Konsept profili aktif. Stil: {PipelineLiveLog.Shorten(conceptProfile.VisualStyleName, 100)}, hedef kitle: {PipelineLiveLog.Shorten(conceptProfile.Audience, 120)}");
            await logAsync($"Konu üretimi hedef kontratı: {productionTarget.DurationSec} sn, ideal sahne {productionTarget.IdealSceneCount}, ideal kelime {productionTarget.IdealNarrationWords}.");

            // 1. Prepare AI Client
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 2. Prepare Prompt (System + User)
            var systemInstruction = TopicPromptDefaults.ResolveSystemInstruction(preset.SystemInstruction) +
                                    "\nIMPORTANT: Return ONLY a raw JSON object (no markdown, no code fences) with these fields: 'Title', 'Premise', 'Category', 'VisualIdea', 'AudiencePromise', 'CentralQuestion', 'Angle', 'KeyPoints', 'ChapterHints', 'AvoidNotes'.";

            var userPrompt = ApplyPromptPlaceholders(TopicPromptDefaults.ResolvePromptTemplate(preset.PromptTemplate), preset, brief, conceptProfile);

            // Add Context Keywords if available
            if (!string.IsNullOrEmpty(preset.ContextKeywordsJson))
            {
                userPrompt += $"\nKeywords/Context: {preset.ContextKeywordsJson}";
            }

            userPrompt += $"""

{productionTarget.ToPromptBlock()}
""";

            if (brief != null)
            {
                userPrompt += $"""

{brief.ToPromptBlock()}

Long-form topic mode:
- Treat the main title as the fixed production anchor. Do not drift away from it.
- Convert the brief into a production-ready topic document, not a random new idea.
- Premise should explain the exact promise of the video.
- KeyPoints and ChapterHints should help the script stage build scenes later.
- AvoidNotes should preserve the user's constraints.
""";
            }

            var topicContext = ProductionPromptContext.BuildTopicContextBlock(conceptProfile);
            if (!string.IsNullOrWhiteSpace(topicContext))
            {
                userPrompt += $"""

{topicContext}

Topic generation rules from concept profile:
- Keep the topic aligned with the channel promise and target audience.
- Use content rules as durable constraints.
- Visual idea should be compatible with the concept visual style.
""";
            }

            // 3. Call AI
            await logAsync("AI'ya konu üretim isteği gönderiliyor.");

            var responseText = await aiClient.GenerateTextAsync(
                prompt: $"{systemInstruction}\n\nUser Request: {userPrompt}",
                temperature: preset.Temperature,
                model: preset.ModelName,
                ct: ct
            );

            await logAsync("AI yanıtı alındı. Konu JSON çıktısı işleniyor.");

            // 4. Parse JSON (Smart Try)
            string title = "Untitled Topic";
            string premise = responseText;
            string? category = null;
            string? visualIdea = null;
            string audiencePromise = "";
            string centralQuestion = "";
            string angle = "";
            var keyPoints = new List<string>();
            var chapterHints = new List<string>();
            string avoidNotes = "";

            try
            {
                // Clean Markdown (e.g. ```json ... ```)
                var cleanJson = responseText.Replace("```json", "").Replace("```", "").Trim();

                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                title = GetStr(root, "Title", "title", "VideoTitle") is { Length: > 0 } parsedTitle ? parsedTitle : title;
                premise = GetStr(root, "Premise", "premise", "Summary", "summary") is { Length: > 0 } parsedPremise ? parsedPremise : premise;
                category = GetStr(root, "Category", "category");
                visualIdea = GetStr(root, "VisualIdea", "visualIdea", "VisualDirection", "visualDirection");
                audiencePromise = GetStr(root, "AudiencePromise", "audiencePromise", "Promise", "promise");
                centralQuestion = GetStr(root, "CentralQuestion", "centralQuestion", "Question", "question");
                angle = GetStr(root, "Angle", "angle", "Thesis", "thesis");
                keyPoints = GetStringList(root, "KeyPoints", "keyPoints", "MustCover", "mustCover");
                chapterHints = GetStringList(root, "ChapterHints", "chapterHints", "Chapters", "chapters");
                avoidNotes = GetStr(root, "AvoidNotes", "avoidNotes", "Avoid", "avoid");
            }
            catch
            {
                await logAsync(PipelineLiveLog.Warning("AI yanıtı geçerli JSON değil. Ham metin konu açıklaması olarak kullanılacak."));
                // Fallback: Use full text as premise, truncate for title
                title = responseText.Length > 50 ? responseText.Substring(0, 47) + "..." : responseText;
            }

            if (brief != null)
            {
                if (!string.IsNullOrWhiteSpace(brief.MainTitle)) title = brief.MainTitle.Trim();
                if (string.IsNullOrWhiteSpace(angle)) angle = brief.Angle;
                if (string.IsNullOrWhiteSpace(avoidNotes)) avoidNotes = brief.Avoid;
                if (string.IsNullOrWhiteSpace(audiencePromise) && !string.IsNullOrWhiteSpace(brief.Audience))
                    audiencePromise = $"Designed for: {brief.Audience}";
            }

            var topicDocument = BuildTopicDocument(
                title,
                premise,
                audiencePromise,
                centralQuestion,
                angle,
                keyPoints,
                chapterHints,
                visualIdea,
                avoidNotes,
                brief,
                conceptProfile);

            // 5. Save to DB (Topic Library)
            var topic = new Topic
            {
                AppUserId = run.AppUserId,
                Title = title,
                Premise = premise,
                Category = category,
                VisualPromptHint = visualIdea,
                LanguageCode = preset.Language,
                SourcePresetId = preset.Id,
                CreatedByRunId = run.Id,
                RawJsonData = responseText, // Save raw data for debugging
                CreatedAt = DateTime.UtcNow,
                ConceptId = run.Template.ConceptId,
            };

            await _topicRepo.AddAsync(topic, ct);
            await _uow.SaveChangesAsync(ct);

            await logAsync(PipelineLiveLog.Success($"Konu veritabanına kaydedildi. ID: {topic.Id}, başlık: '{title}'."));

            // 6. Return Payload for Next Stage
            return new TopicStagePayload
            {
                TopicId = topic.Id,
                TopicTitle = topic.Title,
                TopicText = topicDocument, // Script stage will use this
                AudiencePromise = audiencePromise,
                CentralQuestion = centralQuestion,
                Angle = angle,
                KeyPoints = keyPoints,
                ChapterHints = chapterHints,
                AvoidNotes = avoidNotes,
                Brief = brief,
                Language = ProductionPromptContext.ResolveLanguage(conceptProfile, preset.Language)
            };
        }

        private static string ApplyPromptPlaceholders(
            string template,
            TopicPreset preset,
            ProductionBrief? brief,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var result = template
                .Replace("{Language}", preset.Language)
                .Replace("{ModelName}", preset.ModelName);

            result = ProductionPromptContext.ApplyPlaceholders(result, conceptProfile);

            if (brief == null) return result;

            return result
                .Replace("{MainTitle}", brief.MainTitle)
                .Replace("{BriefTitle}", brief.MainTitle)
                .Replace("{Angle}", brief.Angle)
                .Replace("{Audience}", brief.Audience)
                .Replace("{TargetDuration}", brief.TargetDuration)
                .Replace("{MustCover}", brief.MustCover)
                .Replace("{Avoid}", brief.Avoid)
                .Replace("{Notes}", brief.Notes);
        }

        private static string BuildTopicDocument(
            string title,
            string premise,
            string audiencePromise,
            string centralQuestion,
            string angle,
            List<string> keyPoints,
            List<string> chapterHints,
            string? visualIdea,
            string avoidNotes,
            ProductionBrief? brief,
            Application.Contracts.ConceptProfiles.ConceptProfileDto? conceptProfile)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Title: {title}");
            Append(sb, "Premise", premise);
            Append(sb, "Audience promise", audiencePromise);
            Append(sb, "Central question", centralQuestion);
            Append(sb, "Angle / thesis", angle);
            AppendList(sb, "Key points", keyPoints);
            AppendList(sb, "Chapter hints", chapterHints);
            Append(sb, "Visual direction", visualIdea);
            Append(sb, "Avoid notes", avoidNotes);

            if (brief != null)
            {
                sb.AppendLine();
                sb.AppendLine(brief.ToPromptBlock());
            }

            var conceptBlock = ProductionPromptContext.BuildTopicContextBlock(conceptProfile);
            if (!string.IsNullOrWhiteSpace(conceptBlock))
            {
                sb.AppendLine();
                sb.AppendLine(conceptBlock);
            }

            return sb.ToString().Trim();
        }

        private static void Append(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{label}: {value.Trim()}");
        }

        private static void AppendList(StringBuilder sb, string label, List<string> values)
        {
            var clean = values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            if (clean.Count == 0) return;
            sb.AppendLine($"{label}:");
            foreach (var item in clean)
                sb.AppendLine($"- {item}");
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

            return raw
                .Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim('-', ' ', '\t'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ToPlainString(JsonElement el)
            => el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.GetRawText();

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
    }
}
