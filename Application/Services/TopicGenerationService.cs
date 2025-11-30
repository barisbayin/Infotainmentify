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
    /// TopicGenerationProfile kayıtlarını kullanarak AI tabanlı topic üretimi yapan servis.
    /// </summary>
    /// <summary>
    /// TopicGenerationProfile kayıtlarını kullanarak AI tabanlı topic üretimi yapan servis.
    /// </summary>
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
        /// Seçilen profilden yeni topic'ler üretir ve DB'ye kaydeder.
        /// </summary>
        public async Task<string> GenerateFromProfileAsync(int profileId, CancellationToken ct)
        {
            /*
            var userId = _current.UserId;

            var profile = await _profileRepo.GetByIdAsync(profileId, true, ct)
                ?? throw new InvalidOperationException("Topic generation profili bulunamadı.");

            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");

            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif durumda, işlem yapılamaz.");

            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");

            if (!prompt.IsActive)
                throw new InvalidOperationException("Prompt pasif durumda, topic üretimi yapılamaz.");

            // 🔹 Kullanıcı fallback
            if (userId <= 0)
                userId = profile.AppUserId;

            // 🔹 Credential çöz
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            var systemPrompt = (prompt.SystemPrompt ?? string.Empty)
                .Replace("{{COUNT}}", profile.RequestedCount.ToString())
                .Replace("{{LANGUAGE}}", profile.Language ?? "en-US")
                .Replace("{{COUNT * 2}}", (profile.RequestedCount * 2).ToString());

            var userPrompt = (prompt.Body ?? string.Empty)
                .Replace("{{COUNT}}", profile.RequestedCount.ToString())
                .Replace("{{LANGUAGE}}", profile.Language ?? "en-US")
                .Replace("{{COUNT * 2}}", (profile.RequestedCount * 2).ToString());

            // 🔹 Topic üretim isteği hazırla
            var request = new TopicGenerationRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Count = profile.RequestedCount,
                Model = profile.ModelName ?? creds.GetValueOrDefault("model", aiConn.TextModel),
                Temperature = profile.Temperature,
                Language = profile.Language ?? "en",
                MaxTokens = profile.MaxTokens,
                OutputMode = profile.OutputMode ?? "Topic",
                ProductionType = profile.ProductionType ?? "shorts",
                RenderStyle = profile.RenderStyle ?? "default",
                TagsJson = profile.TagsJson,
                ProfileId = profile.Id,
                UserId = userId,
                ExtraParameters = creds
            };

            // 🔹 AI'dan konu üret
            var topics = await generator.GenerateTopicsAsync(request, ct);

            // 🔹 Retry özelliği
            if (topics.Count == 0 && profile.AllowRetry)
                topics = await generator.GenerateTopicsAsync(request, ct);

            if (topics.Count == 0)
                return "Hiç konu üretilmedi.";

            var now = DateTime.Now;

            // 🔹 Üretilen topic'leri veritabanına dönüştür
            var entities = topics.Select((t, i) => new Topic
            {
                UserId = userId,
                PromptId = prompt.Id,
                TopicCode = $"T{profileId}_{now:yyyyMMddHHmmss}_{i:D3}",

                Category = t.Category ?? "general",
                SubCategory = t.SubCategory,
                Series = t.Series,
                Premise = t.Premise,
                PremiseTr = t.PremiseTr,
                Tone = t.Tone,
                PotentialVisual = t.PotentialVisual,
                RenderStyle = t.RenderStyle ?? profile.RenderStyle,
                VoiceHint = t.VoiceHint,
                ScriptHint = t.ScriptHint,
                FactCheck = t.FactCheck,
                NeedsFootage = t.NeedsFootage,
                Priority = t.Priority,

                TopicJson = JsonSerializer.Serialize(t),
                ScriptGenerated = false,
                ScriptGeneratedAt = null,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            // 🔹 Kayıtları ekle
            await _topicRepo.AddRangeAsync(entities, ct);
            await _uow.SaveChangesAsync(ct);

            // 🔹 Opsiyonel script üretimi
            if (profile.AutoGenerateScript)
            {
                // TODO: ScriptGenerationService entegrasyonu
                // await _scriptService.GenerateForTopicsAsync(entities.Select(x => x.Id), ct);
            }
            */
            return $"{0} adet konu başarıyla üretildi.";
        }

        public async Task<Topic> GenerateSingleAsync(int topicProfileId, CancellationToken ct)
        {
            /*
            var userId = _current.UserId;

            // Profil al
            var profile = await _profileRepo.GetByIdAsync(topicProfileId, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("TopicGenerationProfile bulunamadı.");

            // Prompt
            var prompt = await _promptRepo.GetByIdAsync(profile.PromptId, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("Prompt bulunamadı.");

            // AI bağlantısı
            var aiConn = await _aiRepo.GetByIdAsync(profile.AiConnectionId, asNoTracking: true, ct)
                ?? throw new InvalidOperationException("AI bağlantısı bulunamadı.");

            if (!aiConn.IsActive)
                throw new InvalidOperationException("AI bağlantısı pasif, işlem yapılamaz.");

            // Credential çöz
            var credsJson = _secret.Unprotect(aiConn.EncryptedCredentialJson);
            var creds = JsonSerializer.Deserialize<Dictionary<string, string>>(credsJson)
                ?? throw new InvalidOperationException("Geçersiz credential formatı.");

            var generator = _factory.Resolve(aiConn.Provider, creds);

            // System prompt + user prompt hazırlama (Count = 1)
            var systemPrompt = (prompt.SystemPrompt ?? string.Empty)
                .Replace("{{COUNT}}", "1")
                .Replace("{{LANGUAGE}}", profile.Language ?? "en-US")
                .Replace("{{COUNT * 2}}", "2");

            var userPrompt = (prompt.Body ?? string.Empty)
                .Replace("{{COUNT}}", "1")
                .Replace("{{LANGUAGE}}", profile.Language ?? "en-US")
                .Replace("{{COUNT * 2}}", "2");

            // Tek topic üretim isteği
            var request = new TopicGenerationRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Count = 1,
                Model = profile.ModelName ?? creds.GetValueOrDefault("model", aiConn.TextModel),
                Temperature = profile.Temperature,
                Language = profile.Language ?? "en",
                MaxTokens = profile.MaxTokens,
                OutputMode = profile.OutputMode ?? "Topic",
                ProductionType = profile.ProductionType ?? "shorts",
                RenderStyle = profile.RenderStyle ?? "default",
                TagsJson = profile.TagsJson,
                ProfileId = profile.Id,
                UserId = userId,
                ExtraParameters = creds
            };

            // ---- AI çağrısı ----
            var list = await generator.GenerateTopicsAsync(request, ct);
            if (list.Count == 0)
                throw new Exception("Topic üretilemedi.");

            var gen = list.First();

            // ---- Topic entity üret ----
            var now = DateTime.Now;

            var topic = new Topic
            {
                UserId = userId,
                PromptId = prompt.Id,
                TopicCode = $"T{topicProfileId}_{now:yyyyMMddHHmmss}_001",

                // AI'dan gelen alanlar
                Premise = gen.Premise,
                PremiseTr = gen.PremiseTr,
                Category = gen.Category ?? "general",
                SubCategory = gen.SubCategory,
                Series = gen.Series,
                Tone = gen.Tone,
                PotentialVisual = gen.PotentialVisual,
                RenderStyle = gen.RenderStyle ?? profile.RenderStyle,
                VoiceHint = gen.VoiceHint,
                ScriptHint = gen.ScriptHint,
                FactCheck = gen.FactCheck,
                NeedsFootage = gen.NeedsFootage,
                Priority = gen.Priority,

                TopicJson = JsonSerializer.Serialize(gen),

                ScriptGenerated = false,
                ScriptGeneratedAt = null,

                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            // DB'ye ekle
            await _topicRepo.AddAsync(topic, ct);
            await _uow.SaveChangesAsync(ct);

            return topic;
            */

            return new Topic();
        }
    }
}
