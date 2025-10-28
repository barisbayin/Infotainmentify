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
        private readonly ISecretStore _secret;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public TopicGenerationService(
            IAiGeneratorFactory factory,
            IRepository<UserAiConnection> aiRepo,
            IRepository<Prompt> promptRepo,
            IRepository<Topic> topicRepo,
            ISecretStore secret,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _factory = factory;
            _aiRepo = aiRepo;
            _promptRepo = promptRepo;
            _topicRepo = topicRepo;
            _secret = secret;
            _uow = uow;
            _current = current;
        }

        /// <summary>
        /// Seçilen prompt ve AI bağlantısına göre topic üretir ve DB'ye kaydeder.
        /// </summary>
        public async Task GenerateAndSaveAsync(int aiConnId, int promptId, int count, CancellationToken ct)
        {
            var userId = _current.UserId;

            // --- AI bağlantısı ve prompt'u al ---
            var aiConn = await _aiRepo.GetByIdAsync(aiConnId, true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");
            var prompt = await _promptRepo.GetByIdAsync(promptId, true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");

            // --- Credential çöz ---
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            // --- Uygun generator’u al ---
            var generator = _factory.Resolve(aiConn.Provider, creds);

            // --- AI'dan topic iste ---
            var topics = await generator.GenerateTopicsAsync(
                systemPrompt: prompt.Name,
                userPrompt: prompt.Body,
                count: count,
                model: creds.GetValueOrDefault("model", aiConn.TextModel),
                temperature: double.TryParse(creds.GetValueOrDefault("temperature"), out var t)? t : (aiConn.Temperature.HasValue ? (double)aiConn.Temperature.Value : 0.8D),
                ct: ct);

            if (topics.Count == 0)
                return;

            // --- DB'ye yaz ---
            var entities = topics.Select(t => new Topic
            {
                UserId = userId,
                PromptId = promptId,
                TopicCode = t.Id ?? Guid.NewGuid().ToString("N")[..12],
                Category = t.Category,
                Premise = t.Premise,
                Tone = t.Tone,
                PotentialVisual = t.PotentialVisual,
                NeedsFootage = t.NeedsFootage,
                FactCheck = t.FactCheck,
                TagsJson = JsonSerializer.Serialize(t.Tags ?? Array.Empty<string>()),
                TopicJson = JsonSerializer.Serialize(t)
            }).ToList();

            await _topicRepo.AddRangeAsync(entities, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }

}
