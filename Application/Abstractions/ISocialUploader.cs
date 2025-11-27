using Application.Contracts.Script;

namespace Application.Abstractions
{
    public interface ISocialUploader
    {
        /// <summary>
        /// Final video dosyasını platforma yükler ve platform video ID’si döner.
        /// </summary>
        Task<string> UploadAsync(
           int userId,
           string videoPath,
           ScriptContentDto script,
           IReadOnlyDictionary<string, string> credentials,
           CancellationToken ct = default
       );
    }
}
