using Application.Abstractions;
using Application.AiLayer;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
using System.Text.Json;

namespace Application.Services
{
    public class TopicGenerationService
    {
        private readonly IAiGeneratorFactory _factory;
        private readonly IRepository<UserAiConnection> _aiRepo;
        private readonly IRepository<Prompt> _promptRepo;
        private readonly IRepository<Topic> _topicRepo;
        private readonly IRepository<TopicGenerationProfile> _profileRepo;
        private readonly ISecretStore _secret;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public TopicGenerationService(
            IAiGeneratorFactory factory,
            IRepository<UserAiConnection> aiRepo,
            IRepository<Prompt> promptRepo,
            IRepository<Topic> topicRepo,
            IRepository<TopicGenerationProfile> profileRepo,
            ISecretStore secret,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _factory = factory;
            _aiRepo = aiRepo;
            _promptRepo = promptRepo;
            _topicRepo = topicRepo;
            _profileRepo = profileRepo;
            _secret = secret;
            _uow = uow;
            _current = current;
        }

        /// <summary>
        /// Seçilen prompt ve AI bağlantısına göre topic üretir ve DB'ye kaydeder.
        /// </summary>
        public async Task<string> GenerateFromProfileAsync(int profileId, CancellationToken ct)
        {
            var userId = _current.UserId;

            var profile = await _profileRepo.GetByIdAsync(profileId, true, ct)
                ?? throw new InvalidOperationException("Topic generation profili bulunamadı.");
            if (!profile.IsActive)
                throw new InvalidOperationException("Bu profil pasif durumda, topic üretimi yapılamaz.");

            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");
            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif durumda, işlem yapılamaz.");

            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");
            if (!prompt.IsActive)
                throw new InvalidOperationException("Prompt pasif durumda, topic üretimi yapılamaz.");

            // user fallback
            if (userId <= 0)
                userId = profile.AppUserId;

            // --- Credential çöz ---
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);
            var temperature = (double)(aiConn.Temperature ?? 0.8M);

            var topics = await generator.GenerateTopicsAsync(
                systemPrompt: prompt.Name,
                userPrompt: prompt.Body,
                count: profile.RequestedCount,
                model: creds.GetValueOrDefault("model", aiConn.TextModel),
                temperature: temperature,
                ct: ct);

            if (topics.Count == 0)
                return "Hiç konu üretilmedi.";

            var entities = topics.Select((t, i) => new Topic
            {
                UserId = userId,
                PromptId = prompt.Id,
                TopicCode = $"T_P_{profileId}_{DateTime.Now:yyyyMMddHHmmss}_{i:D3}",
                Category = t.Category,
                Premise = t.Premise,
                PremiseTr = t.PremiseTr,
                Tone = t.Tone,
                PotentialVisual = t.PotentialVisual,
                NeedsFootage = t.NeedsFootage,
                FactCheck = t.FactCheck,
                TagsJson = JsonSerializer.Serialize(t.Tags ?? Array.Empty<string>()),
                TopicJson = JsonSerializer.Serialize(t)
            }).ToList();

            await _topicRepo.AddRangeAsync(entities, ct);
            await _uow.SaveChangesAsync(ct);

            return $"{entities.Count} adet konu başarıyla üretildi.";
        }


    }

}
