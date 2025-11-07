using Application.Abstractions;
using Application.AiLayer;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using System.Text.Json;

namespace Application.Services
{
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
        /// Seçilen ScriptGenerationProfile’a göre, ilgili Topic(ler) için script üretir.
        /// </summary>
        public async Task<string> GenerateFromProfileAsync(int profileId, CancellationToken ct)
        {
            var userId = _current.UserId;

            var profile = await _profileRepo.GetByIdAsync(profileId, true, ct)
                ?? throw new InvalidOperationException("Script generation profili bulunamadı.");
            if (!profile.IsActive)
                throw new InvalidOperationException("Bu profil pasif durumda, script üretimi yapılamaz.");

            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");
            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif durumda.");

            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");
            if (!prompt.IsActive)
                throw new InvalidOperationException("Prompt pasif durumda.");

            // Kullanıcı fallback
            if (userId <= 0)
                userId = profile.AppUserId;

            // --- Credential çöz ---
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            // --- Üretilecek topic’leri belirle ---
            List<Topic> topics;

            if (!string.IsNullOrEmpty(profile.TopicIdsJson))
            {
                var topicIds = JsonSerializer.Deserialize<List<int>>(profile.TopicIdsJson) ?? new();
                topics = (await _topicRepo.FindAsync(
                    t => topicIds.Contains(t.Id) && t.UserId == userId,
                    asNoTracking: true,
                    ct: ct)).ToList();
            }
            else if (profile.TopicGenerationProfileId.HasValue)
            {
                topics = (await _topicRepo.FindAsync(
                    t => t.UserId == userId && t.PromptId == prompt.Id,
                    asNoTracking: true,
                    ct: ct)).ToList();
            }
            else
            {
                throw new InvalidOperationException("Hiçbir topic tanımlanmadı.");
            }

            if (topics.Count == 0)
                throw new InvalidOperationException("Script üretilecek topic bulunamadı.");

            // --- Ortak AI parametreleri ---
            var temperature = profile.Temperature > 0 ? profile.Temperature : 0.8;
            var model = creds.GetValueOrDefault("model", aiConn.TextModel);
            var productionType = profile.ProductionType ?? "video";
            var renderStyle = profile.RenderStyle ?? "cinematic_vertical";
            var language = profile.Language ?? "en";

            int successCount = 0;
            foreach (var topic in topics)
            {
                // 🔹 Prompt Body’yi parametrelerle doldur
                var finalPrompt = prompt.Body
                    .Replace("{{PREMISE}}", topic.Premise ?? "")
                    .Replace("{{CATEGORY}}", topic.Category ?? "")
                    .Replace("{{TONE}}", topic.Tone ?? "")
                    .Replace("{{LANGUAGE}}", language)
                    .Replace("{{PRODUCTION_TYPE}}", productionType)
                    .Replace("{{RENDER_STYLE}}", renderStyle)
                    .Replace("{{POTENTIAL_VISUAL}}", topic.PotentialVisual ?? "")
                    //.Replace("{{TAGS}}", topic.TagsJson ?? "[]")
                    .Replace("{{CONFIG}}", profile.ConfigJson ?? "{}");

                try
                {
                    var result = await generator.GenerateTextAsync(finalPrompt, temperature, model, ct);

                    // Script entity oluştur
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
                    successCount++;
                }
                catch (Exception ex)
                {
                    // AI hata loglaması (isteğe göre DB log tablosuna alınabilir)
                    Console.WriteLine($"Script üretim hatası: {ex.Message}");
                }
            }

            await _uow.SaveChangesAsync(ct);
            return $"{successCount} script başarıyla üretildi.";
        }
    }
}
