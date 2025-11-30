using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Models;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Entity.User;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// Seçilen ScriptGenerationProfile’a göre ilgili Topic(ler) için script üretimi yapar.
    /// Gerçek zamanlı ilerleme ve tamamlanma bilgilerini INotifierService üzerinden SignalR’a gönderir.
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
        private readonly INotifierService _notifier;

        public ScriptGenerationService(
            IAiGeneratorFactory factory,
            IRepository<UserAiConnection> aiRepo,
            IRepository<Prompt> promptRepo,
            IRepository<Script> scriptRepo,
            IRepository<ScriptGenerationProfile> profileRepo,
            IRepository<Topic> topicRepo,
            ISecretStore secret,
            IUnitOfWork uow,
            ICurrentUserService current,
            INotifierService notifier) // 🔹 yeni eklendi
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
            _notifier = notifier;
        }

        /// <summary>
        /// Seçilen ScriptGenerationProfile’a göre script üretir ve DB’ye kaydeder.
        /// </summary>
        public async Task<ScriptGenerationResult> GenerateFromProfileAsync(int profileId, CancellationToken ct)
        {
            /*
            var userId = _current.UserId;
            var jobId = profileId; // job id olarak profile id kullanılabilir

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

            var temperature = profile.Temperature > 0 ? profile.Temperature : 0.8;
            var model = profile.ModelName ?? creds.GetValueOrDefault("model", aiConn.TextModel);
            var language = profile.Language ?? "en";
            var productionType = profile.ProductionType ?? "video";
            var renderStyle = profile.RenderStyle ?? "cinematic_vertical";

            var generatedIds = new List<int>();
            var failedIds = new List<int>();

            var systemPrompt = (prompt.SystemPrompt ?? string.Empty)
                .Replace("{{LANGUAGE}}", language)
                .Replace("{{PRODUCTION_TYPE}}", productionType)
                .Replace("{{RENDER_STYLE}}", renderStyle);

            var userPrompt = (prompt.Body ?? string.Empty)
                .Replace("{{LANGUAGE}}", language)
                .Replace("{{PRODUCTION_TYPE}}", productionType)
                .Replace("{{RENDER_STYLE}}", renderStyle);

            int processed = 0;
            foreach (var topic in topics)
            {
                processed++;
                var progress = (int)((double)processed / topics.Count * 100);

                await _notifier.JobProgressAsync(userId, jobId, $"Topic {topic.TopicCode} işleniyor", progress);

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
                    Premise = topic.Premise,
                    Tone = topic.Tone,
                    PotentialVisual = topic.PotentialVisual
                };

                try
                {
                    var resultScript = await generator.GenerateScriptsAsync(request, ct);

                    if (string.IsNullOrEmpty(resultScript) && profile.AllowRetry)
                        resultScript = await generator.GenerateScriptsAsync(request, ct);

                    if (string.IsNullOrEmpty(resultScript))
                    {
                        failedIds.Add(topic.Id);
                        continue;
                    }


                    var entity = new Script
                    {
                        UserId = userId,
                        TopicId = topic.Id,
                        Title = $"Script for {topic.TopicCode}",
                        Content = resultScript,
                        Summary = null,
                        Language = language,
                        MetaJson =
                             JsonSerializer.Serialize(new
                             {
                                 model,
                                 temperature,
                                 aiConn.Provider,
                                 productionType,
                                 renderStyle
                             }),
                        ScriptGenerationProfileId = profile.Id,
                    };

                    await _scriptRepo.AddAsync(entity, ct);


                    topic.ScriptGenerated = true;
                    topic.ScriptGeneratedAt = DateTimeOffset.Now;
                    generatedIds.Add(topic.Id);

                    Thread.Sleep(15000);
                }
                catch (Exception ex)
                {
                    failedIds.Add(topic.Id);
                    await _notifier.NotifyUserAsync(userId, "JobError", new
                    {
                        topicId = topic.Id,
                        error = ex.Message
                    });
                }
            }

            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(
                userId,
                jobId,
                failedIds.Count == 0,
                $"Script üretimi tamamlandı. Başarılı: {generatedIds.Count}, Hatalı: {failedIds.Count}");

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
            */
            return new ScriptGenerationResult
            {
               
            };
        }

        /// <summary>
        /// Belirli topic’ler için script üretimi (manuel seçimle).
        /// </summary>
        public async Task<ScriptGenerationResult> GenerateForTopicsAsync(
            int profileId,
            IReadOnlyList<int> topicIds,
            CancellationToken ct)
        {
            /*
            var userId = _current.UserId;
            var jobId = profileId;

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

            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            var topics = (await _topicRepo.FindAsync(
                t => topicIds.Contains(t.Id)
                  && t.UserId == userId
                  && t.AllowScriptGeneration
                  && !t.ScriptGenerated,
                asNoTracking: false,
                ct: ct)).ToList();

            if (topics.Count == 0)
                throw new InvalidOperationException("Üretilebilecek uygun topic bulunamadı.");

            var temperature = profile.Temperature > 0 ? profile.Temperature : 0.8;
            var model = creds.GetValueOrDefault("model", aiConn.TextModel);
            var productionType = profile.ProductionType ?? "video";
            var renderStyle = profile.RenderStyle ?? "cinematic_vertical";
            var language = profile.Language ?? "en";

            var generatedIds = new List<int>();
            var failedIds = new List<int>();

            int processed = 0;
            foreach (var topic in topics)
            {
                processed++;
                var progress = (int)((double)processed / topics.Count * 100);

                await _notifier.JobProgressAsync(userId, jobId, $"Topic {topic.TopicCode} işleniyor", progress);

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
                    topic.ScriptGeneratedAt = DateTimeOffset.Now;
                    generatedIds.Add(topic.Id);
                }
                catch (Exception ex)
                {
                    failedIds.Add(topic.Id);
                    await _notifier.NotifyUserAsync(userId, "JobError", new { topicId = topic.Id, error = ex.Message });
                }
            }

            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(
                userId,
                jobId,
                failedIds.Count == 0,
                $"Script üretimi tamamlandı. Başarılı: {generatedIds.Count}, Hatalı: {failedIds.Count}");

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
            */

            return new ScriptGenerationResult();
        }

        public async Task<Script> GenerateSingleAsync(
    int scriptProfileId,
    Topic topic,
    int? userId,
    CancellationToken ct)
        {

            /*
            ct.ThrowIfCancellationRequested();

            userId ??= _current.UserId;

            // 1) ScriptGenerationProfile çek
            var profile = await _profileRepo.GetByIdAsync(scriptProfileId, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("ScriptGenerationProfile bulunamadı.");

            if (!profile.IsActive)
                throw new InvalidOperationException("Bu script profili pasif.");

            // 2) AI connection
            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");

            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif.");

            // 3) Prompt
            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("Script için prompt bulunamadı.");

            if (!prompt.IsActive)
                throw new InvalidOperationException("Prompt pasif durumda.");

            // 4) Credentials çöz
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            // 5) Prompt değişkenlerini doldur
            var language = profile.Language ?? "en";
            var productionType = profile.ProductionType ?? "video";
            var renderStyle = profile.RenderStyle ?? "cinematic_vertical";

            var systemPrompt = (prompt.SystemPrompt ?? string.Empty)
                .Replace("{{LANGUAGE}}", language)
                .Replace("{{PRODUCTION_TYPE}}", productionType)
                .Replace("{{RENDER_STYLE}}", renderStyle);

            var userPrompt = (prompt.Body ?? string.Empty)
                .Replace("{{PREMISE}}", topic.Premise ?? "")
                .Replace("{{CATEGORY}}", topic.Category ?? "")
                .Replace("{{TONE}}", topic.Tone ?? "")
                .Replace("{{LANGUAGE}}", language)
                .Replace("{{POTENTIAL_VISUAL}}", topic.PotentialVisual ?? "")
                .Replace("{{RENDER_STYLE}}", renderStyle)
                .Replace("{{PRODUCTION_TYPE}}", productionType)
                .Replace("{{TOPIC_JSON}}", topic.TopicJson);

            // 6) Script AI request
            var req = new ScriptGenerationRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Model = profile.ModelName ?? aiConn.TextModel,
                Temperature = profile.Temperature > 0 ? profile.Temperature : 0.8,
                Language = language,
                ProductionType = productionType,
                RenderStyle = renderStyle,
                Premise = topic.Premise,
                PotentialVisual = topic.PotentialVisual,
                Category = topic.Category,
                ProfileId = profile.Id,
                UserId = userId,
                ExtraParameters = creds
            };

            // 7) AI çağrısı
            var scriptResult = await generator.GenerateScriptsAsync(req, ct);

            if (string.IsNullOrWhiteSpace(scriptResult))
                throw new InvalidOperationException("AI script üretemedi.");

            // 8) Script DB entity oluştur
            var script = new Script
            {
                UserId = userId.Value,
                TopicId = topic.Id,
                Title = $"Script for {topic.TopicCode}",
                Content = scriptResult,
                ScriptJson = scriptResult,
                Language = language,
                ScriptGenerationProfileId = profile.Id,
                MetaJson = JsonSerializer.Serialize(new
                {
                    aiConn.Provider,
                    model = req.Model,
                    req.Temperature,
                    productionType,
                    renderStyle
                }),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _scriptRepo.AddAsync(script, ct);
            await _uow.SaveChangesAsync(ct);

            return script;

            */

            return new Script();
        }
    }
}
