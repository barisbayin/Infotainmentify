using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
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

            // 2. AI İstemcisi
            var aiClient = await _aiFactory.ResolveTextClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);

            // =================================================================
            // 🔥 REVİZE 1: PROMPT YAPISI (Array yerine Object istiyoruz)
            // =================================================================
            var systemPrompt = !string.IsNullOrWhiteSpace(preset.SystemInstruction)
                ? preset.SystemInstruction
                : "You are an expert video scriptwriter.";

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
            var userPrompt = preset.PromptTemplate
                .Replace("{Topic}", topicPayload.TopicText)
                .Replace("{Tone}", preset.Tone)
                .Replace("{Duration}", preset.TargetDurationSec.ToString())
                .Replace("{Language}", preset.Language);

            if (preset.IncludeHook) userPrompt += "\nRequirement: Start with a viral hook.";
            if (preset.IncludeCta) userPrompt += "\nRequirement: End with a call to action.";

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

            var scenes = parsedScript.Scenes;
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
                    EstimatedDuration = 5
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

            return new ScriptSceneItem
            {
                SceneNumber = sceneNumber,
                VisualPrompt = visual,
                AudioText = audio,
                EstimatedDuration = duration
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

        private sealed class ParsedScriptResult
        {
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public List<string> Tags { get; set; } = new();
            public List<ScriptSceneItem> Scenes { get; set; } = new();
        }
    }
}
