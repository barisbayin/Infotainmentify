using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Extensions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
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

            // 🔥 FIX 2: Using logAsync instead of exec.AddLog
            await logAsync($"💡 Starting Topic Generation via {preset.ModelName}...");

            // 1. Prepare AI Client
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 2. Prepare Prompt (System + User)
            var systemInstruction = preset.SystemInstruction +
                                    "\nIMPORTANT: Return ONLY a raw JSON object (no markdown, no code fences) with these fields: 'Title', 'Premise', 'Category', 'VisualIdea'.";

            var userPrompt = preset.PromptTemplate;

            // Add Context Keywords if available
            if (!string.IsNullOrEmpty(preset.ContextKeywordsJson))
            {
                userPrompt += $"\nKeywords/Context: {preset.ContextKeywordsJson}";
            }

            // 3. Call AI
            await logAsync("🤖 Sending request to AI...");

            var responseText = await aiClient.GenerateTextAsync(
                prompt: $"{systemInstruction}\n\nUser Request: {userPrompt}",
                temperature: preset.Temperature,
                model: preset.ModelName,
                ct: ct
            );

            await logAsync("✨ AI response received. Processing...");

            // 4. Parse JSON (Smart Try)
            string title = "Untitled Topic";
            string premise = responseText;
            string? category = null;
            string? visualIdea = null;

            try
            {
                // Clean Markdown (e.g. ```json ... ```)
                var cleanJson = responseText.Replace("```json", "").Replace("```", "").Trim();

                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("Title", out var t)) title = t.GetString() ?? title;
                if (root.TryGetProperty("Premise", out var p)) premise = p.GetString() ?? premise;
                if (root.TryGetProperty("Category", out var c)) category = c.GetString();
                if (root.TryGetProperty("VisualIdea", out var v)) visualIdea = v.GetString();
            }
            catch
            {
                await logAsync("⚠️ Warning: AI response was not valid JSON. Using raw text as premise.");
                // Fallback: Use full text as premise, truncate for title
                title = responseText.Length > 50 ? responseText.Substring(0, 47) + "..." : responseText;
            }

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

            await logAsync($"✅ Topic saved to DB. ID: {topic.Id} - '{title}'");

            // 6. Return Payload for Next Stage
            return new TopicStagePayload
            {
                TopicId = topic.Id,
                TopicTitle = topic.Title,
                TopicText = premise, // Script stage will use this
                Language = preset.Language
            };
        }
    }
}
