using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Attributes;
using Core.Contracts;
using Core.Entity.User;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.AiLayer.Concrete
{
    public class AiGeneratorFactory : IAiGeneratorFactory
    {
        private readonly IServiceProvider _sp;
        private readonly IRepository<UserAiConnection> _connRepo;
        private readonly ISecretStore _secret;

        // Harita: Hangi Enum -> Hangi Class Type'ına gidiyor?
        private readonly Dictionary<AiProviderType, Type> _providerMap;

        public AiGeneratorFactory(
            IServiceProvider sp,
            IRepository<UserAiConnection> connRepo,
            ISecretStore secret)
        {
            _sp = sp;
            _connRepo = connRepo;
            _secret = secret;

            // 1. Reflection ile [AiProvider] attribute'una sahip tüm classları bul
            _providerMap = new Dictionary<AiProviderType, Type>();

            // Mevcut Assembly'deki (Application katmanı) servisleri tara
            var providerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute<AiProviderAttribute>() != null && !t.IsAbstract);

            // 2. Haritayı doldur
            foreach (var type in providerTypes)
            {
                var attr = type.GetCustomAttribute<AiProviderAttribute>();
                if (attr != null)
                {
                    // Aynı provider için iki class varsa hata ver (Çakışma önleme)
                    if (_providerMap.ContainsKey(attr.Provider))
                        throw new Exception($"[AiFactory] {attr.Provider} için birden fazla servis tanımlamışsın! Çakışan: {type.Name}");

                    _providerMap[attr.Provider] = type;
                }
            }
        }

        // =================================================================
        // GENERIC RESOLVER (Sihirli Metod 🪄)
        // =================================================================
        private TService ResolveService<TService>(UserAiConnection conn, string apiKey)
            where TService : class, IBaseAiGenerator // IBaseAiGenerator'dan türemeli
        {
            // 1. Provider haritada var mı?
            if (!_providerMap.TryGetValue(conn.Provider, out var implementationType))
            {
                throw new NotSupportedException($"Sağlayıcı '{conn.Provider}' için sistemde [AiProvider] ile işaretlenmiş bir sınıf bulunamadı.");
            }

            // 2. Servisi DI Container'dan çek
            // (Program.cs'de bu servislerin register edilmiş olması lazım!)
            var serviceObj = _sp.GetRequiredService(implementationType);

            // 3. İstenen arayüzü (Interface) destekliyor mu?
            // Örn: ElevenLabs servisi çağırdık ama IImageGenerator istedik -> Hata vermeli.
            if (serviceObj is not TService typedService)
            {
                throw new NotSupportedException($"Seçilen sağlayıcı ({conn.Provider}), istenen işlemi ({typeof(TService).Name}) desteklemiyor. Bu sınıf bu arayüzü implemente etmemiş.");
            }

            // 4. Initialize et ve döndür
            // Base interface'deki Initialize metodunu çağırıyoruz
            typedService.Initialize(apiKey, conn.ExtraId);

            return typedService;
        }

        // =================================================================
        // PUBLIC METOTLAR (Güncel Interface İsimleriyle)
        // =================================================================

        public async Task<ITextGenerator> ResolveTextClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var (conn, apiKey) = await GetConnectionAndKeyAsync(userId, connectionId, ct);
            return ResolveService<ITextGenerator>(conn, apiKey);
        }

        public async Task<IImageGenerator> ResolveImageClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var (conn, apiKey) = await GetConnectionAndKeyAsync(userId, connectionId, ct);
            return ResolveService<IImageGenerator>(conn, apiKey);
        }

        public async Task<ITtsGenerator> ResolveTtsClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var (conn, apiKey) = await GetConnectionAndKeyAsync(userId, connectionId, ct);
            return ResolveService<ITtsGenerator>(conn, apiKey);
        }

        public async Task<ISttGenerator> ResolveSttClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var (conn, apiKey) = await GetConnectionAndKeyAsync(userId, connectionId, ct);
            return ResolveService<ISttGenerator>(conn, apiKey);
        }

        public async Task<IVideoGenerator> ResolveVideoClientAsync(int userId, int? connectionId, CancellationToken ct = default)
        {
            var (conn, apiKey) = await GetConnectionAndKeyAsync(userId, connectionId, ct);
            return ResolveService<IVideoGenerator>(conn, apiKey);
        }

        // =================================================================
        // HELPER
        // =================================================================
        private async Task<(UserAiConnection, string)> GetConnectionAndKeyAsync(int userId, int? connectionId, CancellationToken ct)
        {
            UserAiConnection? conn = connectionId.HasValue
                ? await _connRepo.GetByIdAsync(connectionId.Value, asNoTracking: true, ct: ct)
                : await _connRepo.FirstOrDefaultAsync(c => c.AppUserId == userId, asNoTracking: true, ct: ct);

            if (conn == null)
                throw new Exception("AI bağlantısı bulunamadı. Lütfen önce bir API anahtarı ekleyin.");

            // EncryptedApiKey boş olamaz (Veritabanı kısıtlaması var ama yine de kontrol)
            if (string.IsNullOrEmpty(conn.EncryptedApiKey))
                throw new Exception($"Bağlantı hatalı: {conn.Name} için API anahtarı yok.");

            var apiKey = _secret.Unprotect(conn.EncryptedApiKey);
            return (conn, apiKey);
        }
    }
}