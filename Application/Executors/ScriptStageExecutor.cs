using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Executors
{
    [StageExecutor(StageType.Script)]
    [StagePreset(typeof(ScriptPreset))]
    public class ScriptStageExecutor : BaseStageExecutor
    {
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
                 CancellationToken ct)
        {
            var preset = (ScriptPreset)presetObj!;
            exec.AddLog($"Starting Script Generation with preset: {preset.Name}");

            // 1. Topic Verisini Çek
            var topicPayload = context.GetOutput<TopicStagePayload>(StageType.Topic);

            if (topicPayload == null || string.IsNullOrEmpty(topicPayload.TopicText))
                throw new InvalidOperationException("Önceki adımdan (Topic) veri alınamadı.");

            exec.AddLog($"Source Topic ID: {topicPayload.TopicId}");

            // 2. AI İstemcisi
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // 3. System Prompt (Preset'ten Geliyor)
            // Eğer kullanıcı SystemInstruction girmemişse varsayılan basit bir şey ekleyebiliriz ama genelde girer.
            // JSON format zorlamasını her zaman ekliyoruz, çünkü kodumuz JSON bekliyor.
            var systemPrompt = !string.IsNullOrWhiteSpace(preset.SystemInstruction)
                ? preset.SystemInstruction
                : "You are an expert video scriptwriter.";

            systemPrompt += "\nIMPORTANT: Output MUST be a valid JSON array of objects (scenes). No markdown.";

            // 4. User Prompt (Preset'ten Geliyor)
            // {Topic}, {Tone}, {Duration} gibi yer tutucuları (placeholders) dolduruyoruz.
            var userPrompt = preset.PromptTemplate
                .Replace("{Topic}", topicPayload.TopicText)
                .Replace("{Tone}", preset.Tone)
                .Replace("{Duration}", preset.TargetDurationSec.ToString())
                .Replace("{Language}", preset.Language);

            // Hook ve CTA bayrakları aktifse promptun sonuna ekleyelim
            if (preset.IncludeHook) userPrompt += "\nRequirement: Start with a viral hook.";
            if (preset.IncludeCta) userPrompt += "\nRequirement: End with a call to action.";

            // 5. AI Çağrısı
            exec.AddLog("Sending prompt to AI...");
            var responseJson = await aiClient.GenerateTextAsync(
                prompt: $"{systemPrompt}\n\n{userPrompt}",
                temperature: 0.7,
                model: preset.ModelName,
                ct: ct
            );

            exec.AddLog("AI response received. Parsing JSON...");

            // 5. JSON Parse ve Temizlik
            var cleanJson = CleanJson(responseJson);
            var scenes = new List<ScriptSceneItem>();
            var fullTextBuilder = new System.Text.StringBuilder();

            try
            {
                using var doc = JsonDocument.Parse(cleanJson);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var audio = GetStr(element, "audio");
                    scenes.Add(new ScriptSceneItem
                    {
                        SceneNumber = GetInt(element, "scene"),
                        VisualPrompt = GetStr(element, "visual"),
                        AudioText = audio,
                        EstimatedDuration = GetInt(element, "duration")
                    });
                    fullTextBuilder.AppendLine(audio);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"AI geçersiz JSON döndürdü. Hata: {ex.Message}\nRaw: {responseJson}");
            }

            // 6. DB'ye Kayıt
            var scriptEntity = new Script
            {
                AppUserId = run.AppUserId,
                TopicId = topicPayload.TopicId,
                Title = topicPayload.TopicText.Length > 50 ? topicPayload.TopicText[..47] + "..." : topicPayload.TopicText, // Geçici başlık
                Content = fullTextBuilder.ToString(),
                ScenesJson = JsonSerializer.Serialize(scenes),
                LanguageCode = preset.Language,
                EstimatedDurationSec = scenes.Sum(x => x.EstimatedDuration),
                SourcePresetId = preset.Id,
                CreatedByRunId = run.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _scriptRepo.AddAsync(scriptEntity, ct);
            await _uow.SaveChangesAsync(ct);

            exec.AddLog($"Script saved. ID: {scriptEntity.Id}, Scenes: {scenes.Count}");

            // 7. Pipeline Devamı İçin Payload Dönüşü
            return new ScriptStagePayload
            {
                ScriptId = scriptEntity.Id,
                Title = scriptEntity.Title,
                FullScriptText = scriptEntity.Content,
                Scenes = scenes
            };
        }

        // --- HELPERS ---
        private string CleanJson(string text)
        {
            text = text.Trim();
            if (text.StartsWith("```json")) text = text.Replace("```json", "").Replace("```", "");
            else if (text.StartsWith("```")) text = text.Replace("```", "");
            return text.Trim();
        }

        private string GetStr(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var p) ? p.GetString() ?? "" : "";

        private int GetInt(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var p) && p.TryGetInt32(out var i) ? i : 5;
    }
}
