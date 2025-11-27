using Application.Abstractions;
using Core.Contracts;
using Core.Entity;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Application.AiLayer
{
    public class AiGeneratorFactory : IAiGeneratorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<UserAiConnection> _connRepo;
        private readonly ISecretStore _secret;

        public AiGeneratorFactory(
            IServiceProvider serviceProvider,
            IRepository<UserAiConnection> connRepo,
            ISecretStore secret)
        {
            _serviceProvider = serviceProvider;
            _connRepo = connRepo;
            _secret = secret;
        }

        public IAiGenerator Resolve(AiProviderType provider, IReadOnlyDictionary<string, string> creds)
        {
            IAiGenerator gen = provider switch
            {
                AiProviderType.OpenAI => _serviceProvider.GetRequiredService<OpenAiClient>(),
                AiProviderType.GoogleVertex => _serviceProvider.GetRequiredService<GeminiAiClient>(),
                _ => throw new NotSupportedException($"Provider '{provider}' not supported.")
            };

            gen.Initialize(creds);
            return gen;
        }

        // ---------------------------------------------------------------------
        // 🧩 Profil bazlı yardımcı metotlar
        // ---------------------------------------------------------------------

        public async Task<IAiGenerator> ResolveImageClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var conn = await GetConnectionAsync(userId, connectionId, ct);
            var creds = ParseDecryptedCreds(conn.EncryptedCredentialJson);
            return Resolve(conn.Provider, creds);
        }

        public async Task<IAiGenerator> ResolveTtsClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var conn = await GetConnectionAsync(userId, connectionId, ct);
            var creds = ParseDecryptedCreds(conn.EncryptedCredentialJson);
            return Resolve(conn.Provider, creds);
        }

        public async Task<IAiGenerator> ResolveSttClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var conn = await GetConnectionAsync(userId, connectionId, ct);
            var creds = ParseDecryptedCreds(conn.EncryptedCredentialJson);
            return Resolve(conn.Provider, creds);
        }

        // ---------------------------------------------------------------------
        // 🔒 Yardımcı fonksiyonlar
        // ---------------------------------------------------------------------

        private IReadOnlyDictionary<string, string> ParseDecryptedCreds(string encryptedJson)
        {
            var decrypted = _secret.Unprotect(encryptedJson);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted);
            if (dict == null || dict.Count == 0)
                throw new InvalidOperationException("AI bağlantı kimlik bilgileri çözümlenemedi.");
            return dict;
        }

        private async Task<UserAiConnection> GetConnectionAsync(int userId, int? connectionId, CancellationToken ct)
        {
            var conn = connectionId.HasValue
                ? await _connRepo.GetByIdAsync(connectionId.Value, true, ct)
                : await _connRepo.FirstOrDefaultAsync(c => c.UserId == userId, true, ct);

            if (conn == null)
                throw new InvalidOperationException("AI bağlantısı bulunamadı.");

            return conn;
        }
    }
}