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

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            CancellationToken ct)
        {
            var preset = (TopicPreset)presetObj!;
            exec.AddLog($"Starting Topic Generation via {preset.ModelName}...");

            // 1. AI İstemcisini Hazırla
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 2. Prompt Hazırlığı (Sistem Talimatı + Kullanıcı İsteği)
            // AI'yı JSON dönmeye zorluyoruz.
            var systemInstruction = preset.SystemInstruction +
                                    "\nIMPORTANT: Return ONLY a raw JSON object (no markdown, no code fences) with these fields: 'Title', 'Premise', 'Category', 'VisualIdea'.";

            var userPrompt = preset.PromptTemplate;

            // Varsa Context Keywordlerini ekle
            if (!string.IsNullOrEmpty(preset.ContextKeywordsJson))
            {
                userPrompt += $"\nKeywords/Context: {preset.ContextKeywordsJson}";
            }

            // 3. AI Çağrısı
            exec.AddLog("Sending request to AI...");
            var responseText = await aiClient.GenerateTextAsync(
                prompt: $"{systemInstruction}\n\nUser Request: {userPrompt}",
                temperature: preset.Temperature,
                model: preset.ModelName,
                ct: ct
            );

            exec.AddLog("AI response received.");

            // 4. JSON Parse (Akıllı Deneme)
            string title = "Untitled Topic";
            string premise = responseText;
            string? category = null;
            string? visualIdea = null;

            try
            {
                // Markdown temizliği (Bazen ```json ... ``` döner)
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
                exec.AddLog("Warning: AI response was not valid JSON. Using raw text as premise.");
                // Parse edemezsek tüm metni Premise olarak kabul ederiz, Title'ı özetleriz.
                title = responseText.Length > 50 ? responseText.Substring(0, 47) + "..." : responseText;
            }

            // 5. DB'ye Kayıt (Topic Kütüphanesi)
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
                RawJsonData = responseText, // Ham veriyi sakla, debug için altın değerinde
                CreatedAt = DateTime.UtcNow
            };

            await _topicRepo.AddAsync(topic, ct);
            await _uow.SaveChangesAsync(ct);

            exec.AddLog($"Topic saved to DB. ID: {topic.Id}");

            // 6. Sonraki Aşamaya Veri Aktarımı
            return new TopicStagePayload
            {
                TopicId = topic.Id,
                TopicText = premise, // Script aşaması bunu kullanacak
                Language = preset.Language
            };
        }
    }
}
