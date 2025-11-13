namespace Application.Abstractions
{
    public interface IFFmpegService
    {
        /// <summary>
        /// Görsel + ses birleşiminden sahne videosu üretir.
        /// </summary>
        Task GenerateSceneVideoAsync(string imagePath, string audioPath, string outputPath, CancellationToken ct = default);

        /// <summary>
        /// Birden fazla sahneyi tek bir final videoda birleştirir.
        /// </summary>
        Task ConcatVideosAsync(IReadOnlyList<string> sceneFiles, string outputPath, CancellationToken ct = default);

        /// <summary>
        /// Videonun süresini (saniye) döner.
        /// </summary>
        Task<double?> GetVideoDurationAsync(string filePath, CancellationToken ct = default);

        /// <summary>
        /// Final videodan thumbnail üretir (ilk kare).
        /// </summary>
        Task<string> GenerateThumbnailAsync(string videoPath, string outputDir, CancellationToken ct = default);
    }
}
