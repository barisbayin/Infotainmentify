using Application.Abstractions;
using Application.AiLayer;
using Application.Models;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// Seçilen ScriptGenerationProfile’a göre ilgili Topic(ler) için script üretimi yapar.
    /// </summary>
    public class ScriptGenerationService
    {
        private readonly IAiGeneratorFactory _factory;
        private readonly IRepository<UserAiConnection> _aiRepo;
        private readonly IRepository<Prompt> _promptRepo;
        private readonly IRepository<Script> _scriptRepo;
        private readonly IRepository<ScriptGenerationProfile> _profileRepo;
        private readonly IRepository<Topic> _topicRepo;
        private readonly ISecretStore _secret;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public ScriptGenerationService(
            IAiGeneratorFactory factory,
            IRepository<UserAiConnection> aiRepo,
            IRepository<Prompt> promptRepo,
            IRepository<Script> scriptRepo,
            IRepository<ScriptGenerationProfile> profileRepo,
            IRepository<Topic> topicRepo,
            ISecretStore secret,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _factory = factory;
            _aiRepo = aiRepo;
            _promptRepo = promptRepo;
            _scriptRepo = scriptRepo;
            _profileRepo = profileRepo;
            _topicRepo = topicRepo;
            _secret = secret;
            _uow = uow;
            _current = current;
        }

        /// <summary>
        /// Seçilen ScriptGenerationProfile’a göre script üretir ve DB’ye kaydeder.
        /// </summary>
        public async Task<ScriptGenerationResult> GenerateFromProfileAsync(int profileId, CancellationToken ct)
        {
            var userId = _current.UserId;

            // 🔹 Profile, Prompt, Connection doğrulama
            var profile = await _profileRepo.GetByIdAsync(profileId, true, ct)
                ?? throw new InvalidOperationException("Script generation profili bulunamadı.");
            if (!profile.IsActive)
                throw new InvalidOperationException("Bu profil pasif durumda.");

            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");
            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif durumda.");

            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");
            if (!prompt.IsActive)
                throw new InvalidOperationException("Prompt pasif durumda.");

            // 🔹 Kullanıcı fallback
            if (userId <= 0)
                userId = profile.AppUserId;

            // 🔹 Credential çöz
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            // 🔹 Uygun topic’leri getir (AllowScriptGeneration = true && ScriptGenerated = false)
            var topics = (await _topicRepo.FindAsync(
                t => t.UserId == userId && t.AllowScriptGeneration && !t.ScriptGenerated,
                asNoTracking: false,
                ct: ct)).ToList();

            if (topics.Count == 0)
                throw new InvalidOperationException("Script üretilecek uygun topic bulunamadı.");

            // 🔹 Ortak parametreler
            var temperature = profile.Temperature > 0 ? profile.Temperature : 0.8;
            var model = profile.ModelName ?? creds.GetValueOrDefault("model", aiConn.TextModel);
            var language = profile.Language ?? "en";
            var productionType = profile.ProductionType ?? "video";
            var renderStyle = profile.RenderStyle ?? "cinematic_vertical";

            var generatedIds = new List<int>();
            var failedIds = new List<int>();

            // 🔹 Prompt’ları hazırla
            var systemPrompt = (prompt.SystemPrompt ?? string.Empty)
                .Replace("{{LANGUAGE}}", language)
                .Replace("{{PRODUCTION_TYPE}}", productionType)
                .Replace("{{RENDER_STYLE}}", renderStyle);

            var userPrompt = (prompt.Body ?? string.Empty)
                .Replace("{{LANGUAGE}}", language)
                .Replace("{{PRODUCTION_TYPE}}", productionType)
                .Replace("{{RENDER_STYLE}}", renderStyle);

            // 🔹 Her topic için üretim
            foreach (var topic in topics)
            {
                var request = new ScriptGenerationRequest
                {
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt,
                    Model = model,
                    Temperature = temperature,
                    Language = language,
                    ProductionType = productionType,
                    RenderStyle = renderStyle,
                    Category = topic.Category,
                    ProfileId = profile.Id,
                    UserId = userId,
                    ExtraParameters = creds,

                    // topic bağlamı
                    Premise = topic.Premise,
                    Tone = topic.Tone,
                    PotentialVisual = topic.PotentialVisual
                };

                try
                {
                    var scripts = await generator.GenerateScriptsAsync(request, ct);

                    // 🔹 Retry özelliği
                    if (scripts.Count == 0 && profile.AllowRetry)
                        scripts = await generator.GenerateScriptsAsync(request, ct);

                    if (scripts.Count == 0)
                    {
                        failedIds.Add(topic.Id);
                        continue;
                    }

                    foreach (var s in scripts)
                    {
                        var entity = new Script
                        {
                            UserId = userId,
                            TopicId = topic.Id,
                            Title = s.Title ?? $"Script for {topic.TopicCode}",
                            Content = s.Content,
                            Summary = s.Summary,
                            Language = s.Language ?? language,
                            MetaJson = s.MetaJson
                                ?? JsonSerializer.Serialize(new
                                {
                                    model,
                                    temperature,
                                    aiConn.Provider,
                                    productionType,
                                    renderStyle
                                })
                        };

                        await _scriptRepo.AddAsync(entity, ct);
                    }

                    topic.ScriptGenerated = true;
                    topic.ScriptGeneratedAt = DateTimeOffset.Now;
                    generatedIds.Add(topic.Id);
                }
                catch (Exception ex)
                {
                    failedIds.Add(topic.Id);
                    Console.WriteLine($"❌ Script üretim hatası (Topic {topic.Id}): {ex.Message}");
                }
            }

            await _uow.SaveChangesAsync(ct);

            // 🔹 Sonuç modelini döndür
            return new ScriptGenerationResult
            {
                TotalRequested = topics.Count,
                SuccessCount = generatedIds.Count,
                FailedCount = failedIds.Count,
                GeneratedTopicIds = generatedIds,
                FailedTopicIds = failedIds,
                Provider = aiConn.Provider.ToString(),
                Model = model,
                Temperature = temperature,
                Language = language,
                ProductionType = productionType,
                RenderStyle = renderStyle
            };
        }

        public async Task<ScriptGenerationResult> GenerateForTopicsAsync(
    int profileId,
    IReadOnlyList<int> topicIds,
    CancellationToken ct)
        {
            var userId = _current.UserId;

            if (topicIds == null || topicIds.Count == 0)
                throw new InvalidOperationException("Topic listesi boş olamaz.");

            var profile = await _profileRepo.GetByIdAsync(profileId, true, ct)
                ?? throw new InvalidOperationException("Script generation profili bulunamadı.");

            if (!profile.IsActive)
                throw new InvalidOperationException("Profil pasif durumda, işlem yapılamaz.");

            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");

            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif durumda.");

            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");

            if (!prompt.IsActive)
                throw new InvalidOperationException("Prompt pasif durumda.");

            if (userId <= 0)
                userId = profile.AppUserId;

            // --- Credential çöz ---
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            // --- İlgili topic’leri al ---
            var topics = (await _topicRepo.FindAsync(
                t => topicIds.Contains(t.Id)
                  && t.UserId == userId
                  && t.AllowScriptGeneration
                  && !t.ScriptGenerated,
                asNoTracking: false,
                ct: ct)).ToList();

            if (topics.Count == 0)
                throw new InvalidOperationException("Üretilebilecek uygun topic bulunamadı.");

            // --- Ortak AI parametreleri ---
            var temperature = profile.Temperature > 0 ? profile.Temperature : 0.8;
            var model = creds.GetValueOrDefault("model", aiConn.TextModel);
            var productionType = profile.ProductionType ?? "video";
            var renderStyle = profile.RenderStyle ?? "cinematic_vertical";
            var language = profile.Language ?? "en";

            var generatedIds = new List<int>();
            var failedIds = new List<int>();

            foreach (var topic in topics)
            {
                var finalPrompt = prompt.Body
                    .Replace("{{PREMISE}}", topic.Premise ?? "")
                    .Replace("{{CATEGORY}}", topic.Category ?? "")
                    .Replace("{{TONE}}", topic.Tone ?? "")
                    .Replace("{{LANGUAGE}}", language)
                    .Replace("{{PRODUCTION_TYPE}}", productionType)
                    .Replace("{{RENDER_STYLE}}", renderStyle)
                    .Replace("{{POTENTIAL_VISUAL}}", topic.PotentialVisual ?? "")
                    .Replace("{{CONFIG}}", profile.ConfigJson ?? "{}");

                try
                {
                    var result = await generator.GenerateTextAsync(finalPrompt, temperature, model, ct);

                    var script = new Script
                    {
                        UserId = userId,
                        TopicId = topic.Id,
                        Title = $"Script for {topic.TopicCode}",
                        Content = result,
                        Language = language,
                        MetaJson = JsonSerializer.Serialize(new
                        {
                            model,
                            temperature,
                            aiConn.Provider,
                            productionType,
                            renderStyle
                        }),
                        ScriptJson = result
                    };

                    await _scriptRepo.AddAsync(script, ct);

                    topic.ScriptGenerated = true;
                    topic.ScriptGeneratedAt = DateTimeOffset.UtcNow;
                    generatedIds.Add(topic.Id);
                }
                catch (Exception ex)
                {
                    failedIds.Add(topic.Id);
                    Console.WriteLine($"❌ Script üretim hatası (Topic {topic.Id}): {ex.Message}");
                }
            }

            await _uow.SaveChangesAsync(ct);

            return new ScriptGenerationResult
            {
                TotalRequested = topics.Count,
                SuccessCount = generatedIds.Count,
                FailedCount = failedIds.Count,
                GeneratedTopicIds = generatedIds,
                FailedTopicIds = failedIds,
                Provider = aiConn.Provider.ToString(),
                Model = model,
                Temperature = temperature,
                Language = language,
                ProductionType = productionType,
                RenderStyle = renderStyle
            };
        }

    }
}
