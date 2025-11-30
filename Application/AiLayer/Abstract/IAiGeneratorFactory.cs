using Core.Enums;

namespace Application.AiLayer.Abstract
{
    public interface IAiGeneratorFactory
    {
        // 🔹 Metin işleri için (ITextAiService -> ITextGenerator OLDU)
        Task<ITextGenerator> ResolveTextClientAsync(int userId, int? connectionId, CancellationToken ct = default);

        // 🔹 Görsel işleri için (IImageAiService -> IImageGenerator OLDU)
        Task<IImageGenerator> ResolveImageClientAsync(int userId, int? connectionId, CancellationToken ct = default);

        // 🔹 Seslendirme işleri için (ITtsAiService -> ITtsGenerator OLDU)
        Task<ITtsGenerator> ResolveTtsClientAsync(int userId, int? connectionId, CancellationToken ct = default);

        // 🔹 STT işleri için (ISttAiService -> ISttGenerator OLDU)
        Task<ISttGenerator> ResolveSttClientAsync(int userId, int? connectionId, CancellationToken ct = default);

        // 🔹 Video işleri için (IVideoAiService -> IVideoGenerator OLDU)
        Task<IVideoGenerator> ResolveVideoClientAsync(int userId, int? connectionId, CancellationToken ct = default);
    }
}
