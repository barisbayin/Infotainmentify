using Core.Enums;

namespace Application.AiLayer
{
    /// <summary>
    /// AI sağlayıcılarını (OpenAI, Gemini vs.) dinamik olarak çözüp,
    /// ilgili istemcileri (IAiGenerator implementasyonları) oluşturan fabrika.
    /// </summary>
    public interface IAiGeneratorFactory
    {
        /// <summary>
        /// Ham provider + credential bilgileri ile doğrudan AI generator oluşturur.
        /// </summary>
        IAiGenerator Resolve(AiProviderType provider, IReadOnlyDictionary<string, string> creds);

        /// <summary>
        /// Kullanıcıya veya profile bağlı AI bağlantısını çözerek
        /// görüntü üretimi için uygun AI client döndürür.
        /// </summary>
        Task<IAiGenerator> ResolveImageClientAsync(int userId, int? connectionId, CancellationToken ct = default);

        /// <summary>
        /// Kullanıcıya veya profile bağlı AI bağlantısını çözerek
        /// TTS (seslendirme) üretimi için uygun AI client döndürür.
        /// </summary>
        Task<IAiGenerator> ResolveTtsClientAsync(int userId, int? connectionId, CancellationToken ct = default);

        Task<IAiGenerator> ResolveSttClientAsync(int userId, int? connectionId, CancellationToken ct = default);
    }
}
