using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Enums;

namespace Application.Executors
{
    [StageExecutor(StageType.Thumbnail)]
    [StagePreset(typeof(ImagePreset))]
    public class ThumbnailStageExecutor : BaseStageExecutor
    {
        private readonly IAiGeneratorFactory _aiFactory;
        private readonly IUserDirectoryService _dirService;

        public ThumbnailStageExecutor(
            IServiceProvider sp,
            IAiGeneratorFactory aiFactory,
            IUserDirectoryService dirService)
            : base(sp)
        {
            _aiFactory = aiFactory;
            _dirService = dirService;
        }

        public override StageType StageType => StageType.Thumbnail;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? presetObj,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            if (presetObj is not ImagePreset preset)
                throw new InvalidOperationException("Thumbnail stage için Image preset seçilmelidir.");

            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            if (scriptData == null)
                throw new InvalidOperationException("Thumbnail için Script çıktısı bulunamadı.");

            await logAsync($"Kapak görseli üretimi hazırlanıyor. Preset: {preset.Name}, model: {preset.ModelName}.");

            var aiClient = await _aiFactory.ResolveImageClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "thumbnails");

            var firstScenePrompt = scriptData.Scenes.FirstOrDefault()?.VisualPrompt ?? scriptData.FullScriptText;
            var thumbnailBrief =
                $"YouTube thumbnail / cover image for a long-form video titled '{scriptData.Title}'. " +
                $"Core visual idea: {firstScenePrompt}. High contrast, cinematic 16:9 composition, one clear focal subject, no text, no logo, no watermark.";

            var finalPrompt = preset.PromptTemplate
                .Replace("{SceneDescription}", thumbnailBrief)
                .Replace("{ArtStyle}", preset.ArtStyle ?? "cinematic editorial")
                .Trim();

            if (string.IsNullOrWhiteSpace(finalPrompt))
                finalPrompt = thumbnailBrief;

            var size = EnsureLandscapeSize(preset.Size);
            await logAsync($"Kapak prompt'u hazırlandı. Boyut: {size}, prompt: {PipelineLiveLog.Shorten(finalPrompt, 220)}");

            var imageBytes = await AiImageRetryPolicy.GenerateImageAsync(
                aiClient: aiClient,
                operationLabel: "Kapak görseli",
                prompt: finalPrompt,
                negativePrompt: preset.NegativePrompt,
                size: size,
                style: preset.ArtStyle,
                model: preset.ModelName,
                logAsync: logAsync,
                ct: ct
            );

            var fileName = $"thumbnail_{run.Id}_{Guid.NewGuid().ToString("N")[..8]}.png";
            var fullPath = Path.Combine(outputDir, fileName);
            await File.WriteAllBytesAsync(fullPath, imageBytes, ct);

            var (width, height) = ParseSize(size);
            await logAsync(PipelineLiveLog.Success($"Kapak görseli hazır. Dosya: {fileName}."));

            return new ThumbnailStagePayload
            {
                ScriptId = scriptData.ScriptId,
                ThumbnailFilePath = fullPath,
                ThumbnailUrl = $"/UserFiles/User_{run.AppUserId}/runs/Run_{run.Id}/thumbnails/{fileName}",
                PromptUsed = finalPrompt,
                Width = width,
                Height = height
            };
        }

        private static string EnsureLandscapeSize(string? size)
        {
            var (width, height) = ParseSize(size);
            if (width > height) return size ?? "1792x1024";
            return "1792x1024";
        }

        private static (int Width, int Height) ParseSize(string? size)
        {
            if (string.IsNullOrWhiteSpace(size)) return (1792, 1024);

            var parts = size
                .ToLowerInvariant()
                .Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 2
                && int.TryParse(parts[0], out var width)
                && int.TryParse(parts[1], out var height))
            {
                return (width, height);
            }

            return size.Contains("16:9", StringComparison.OrdinalIgnoreCase)
                ? (1792, 1024)
                : (1792, 1024);
        }
    }
}
