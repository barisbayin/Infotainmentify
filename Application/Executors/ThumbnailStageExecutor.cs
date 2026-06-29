using Application.Abstractions;
using Application.AiLayer.Abstract;
using Application.Models;
using Application.Pipeline;
using Application.Services;
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
                throw new InvalidOperationException("Thumbnail stage icin Image preset secilmelidir.");

            var scriptData = context.GetOutput<ScriptStagePayload>(StageType.Script);
            if (scriptData == null)
                throw new InvalidOperationException("Thumbnail icin Script ciktisi bulunamadi.");

            var layout = context.HasOutput(StageType.SceneLayout)
                ? context.GetOutput<SceneLayoutStagePayload>(StageType.SceneLayout)
                : null;
            var brief = ProductionBrief.FromJson(run.InputBriefJson);
            var conceptProfile = ProductionPromptContext.GetConceptProfile(run);

            await logAsync($"Kapak gorseli uretimi hazirlaniyor. Preset: {preset.Name}, model: {preset.ModelName}.");

            var aiClient = await _aiFactory.ResolveImageClientAsync(run.AppUserId, preset.UserAiConnectionId, ct);
            var outputDir = await _dirService.GetRunDirectoryAsync(run.AppUserId, run.Id, "thumbnails");

            var firstScenePrompt = scriptData.Scenes.FirstOrDefault()?.VisualPrompt ?? scriptData.FullScriptText;
            var thumbnailBrief =
                $"YouTube thumbnail / cover image for a long-form video titled '{scriptData.Title}'. " +
                $"Core visual idea: {firstScenePrompt}. High contrast, 16:9 composition, one clear focal subject. " +
                $"{ProductionPromptContext.BuildImageContextBlock(conceptProfile)}";

            var finalPrompt = preset.PromptTemplate
                .Replace("{SceneDescription}", thumbnailBrief)
                .Replace("{ArtStyle}", preset.ArtStyle ?? "cinematic editorial")
                .Trim();
            finalPrompt = ProductionPromptContext.ApplyPlaceholders(finalPrompt, conceptProfile);

            if (string.IsNullOrWhiteSpace(finalPrompt))
                finalPrompt = thumbnailBrief;

            var size = EnsureLandscapeSize(preset.Size);
            await logAsync($"Kapak prompt'u hazirlandi. Boyut: {size}, prompt: {PipelineLiveLog.Shorten(finalPrompt, 220)}");

            var imageBytes = await AiImageRetryPolicy.GenerateImageAsync(
                aiClient: aiClient,
                operationLabel: "Kapak gorseli",
                prompt: finalPrompt,
                negativePrompt: ImagePromptComposer.BuildNegativePrompt(preset, null, null, conceptProfile),
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
            var youtubePackage = BuildYouTubePackage(scriptData, layout, brief, finalPrompt);
            await logAsync(PipelineLiveLog.Success($"Kapak gorseli hazir. Dosya: {fileName}."));
            await logAsync($"YouTube paket taslagi hazir. Baslik varyasyonu: {youtubePackage.TitleOptions.Count}, chapter: {youtubePackage.Chapters.Count}, thumbnail konsepti: {youtubePackage.ThumbnailConcepts.Count}.");

            return new ThumbnailStagePayload
            {
                ScriptId = scriptData.ScriptId,
                ThumbnailFilePath = fullPath,
                ThumbnailUrl = $"/UserFiles/User_{run.AppUserId}/runs/Run_{run.Id}/thumbnails/{fileName}",
                PromptUsed = finalPrompt,
                Width = width,
                Height = height,
                YouTubePackage = youtubePackage
            };
        }

        private static YouTubePackagePayload BuildYouTubePackage(
            ScriptStagePayload script,
            SceneLayoutStagePayload? layout,
            ProductionBrief? brief,
            string thumbnailPrompt)
        {
            var title = CleanTitle(script.Title, brief?.MainTitle);
            var tags = NormalizeTags(script.Tags, title, brief);
            var chapters = BuildChapters(script, layout);
            var hashtags = tags
                .Take(5)
                .Select(tag => "#" + new string(tag.Where(char.IsLetterOrDigit).ToArray()))
                .Where(tag => tag.Length > 1)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new YouTubePackagePayload
            {
                TitleOptions = BuildTitleOptions(title, brief),
                ThumbnailConcepts = BuildThumbnailConcepts(title, brief, thumbnailPrompt),
                Description = BuildDescription(script, brief, chapters, hashtags),
                Chapters = chapters,
                Tags = tags,
                Hashtags = hashtags,
                PinnedComment = BuildPinnedComment(title),
                UploadChecklist = new List<string>
                {
                    "Watch the first 30 seconds for hook clarity.",
                    "Check voice loudness and silence warnings from Audio QA.",
                    "Open timeline review and manually inspect fallback / low QA images.",
                    "Pick one title option and one thumbnail concept.",
                    "Verify chapters start at 00:00 and match the final render.",
                    "Confirm thumbnail is readable at small size.",
                    "Upload as private/unlisted first for final YouTube processing check."
                },
                GeneratedAt = DateTime.UtcNow
            };
        }

        private static string CleanTitle(string? scriptTitle, string? briefTitle)
        {
            var title = !string.IsNullOrWhiteSpace(scriptTitle) ? scriptTitle.Trim() : briefTitle?.Trim() ?? "Untitled Video";
            return title.Length <= 100 ? title : title[..97] + "...";
        }

        private static List<string> BuildTitleOptions(string title, ProductionBrief? brief)
        {
            var core = string.IsNullOrWhiteSpace(brief?.MainTitle) ? title : brief.MainTitle.Trim();
            var angle = string.IsNullOrWhiteSpace(brief?.Angle) ? "" : brief.Angle.Trim();

            return new[]
            {
                title,
                $"The Strange Truth About {core}",
                $"Why {core} Is Weirder Than You Think",
                string.IsNullOrWhiteSpace(angle) ? $"{core}: Explained Without the Boring Parts" : $"{core}: {angle}",
                $"I Looked Into {core} and It Got Weird"
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Length <= 100 ? x : x[..97] + "...")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        }

        private static List<ThumbnailConceptItem> BuildThumbnailConcepts(string title, ProductionBrief? brief, string generatedPrompt)
        {
            var subject = string.IsNullOrWhiteSpace(brief?.MainTitle) ? title : brief.MainTitle.Trim();
            var angle = string.IsNullOrWhiteSpace(brief?.Angle) ? "surprising contrast" : brief.Angle.Trim();

            return new List<ThumbnailConceptItem>
            {
                new()
                {
                    Name = "Core Contrast",
                    Prompt = $"16:9 YouTube thumbnail, simple high-contrast visual metaphor for {subject}, clear before/after or cause/effect composition, expressive focal subject, no logo, no watermark.",
                    Rationale = "Ana merak noktasini tek bakista anlatir."
                },
                new()
                {
                    Name = "Question Hook",
                    Prompt = $"16:9 YouTube thumbnail, visual question about {subject}, one confused character or symbolic object facing a surprising clue, clean composition, no text, no logo.",
                    Rationale = "Izleyicide cevap arama istegi olusturur."
                },
                new()
                {
                    Name = "Generated Base",
                    Prompt = generatedPrompt,
                    Rationale = $"Mevcut video stiliyle uyumlu ana kapak. Angle: {angle}"
                }
            };
        }

        private static string BuildDescription(
            ScriptStagePayload script,
            ProductionBrief? brief,
            List<YouTubeChapterItem> chapters,
            List<string> hashtags)
        {
            var description = string.IsNullOrWhiteSpace(script.Description)
                ? $"A long-form infotainment video about {script.Title}."
                : script.Description.Trim();

            if (!string.IsNullOrWhiteSpace(brief?.Angle))
                description += $"\n\nAngle: {brief.Angle.Trim()}";

            if (chapters.Count > 0)
            {
                description += "\n\nChapters:\n";
                description += string.Join("\n", chapters.Select(chapter => $"{chapter.Timestamp} {chapter.Title}"));
            }

            if (hashtags.Count > 0)
                description += "\n\n" + string.Join(" ", hashtags);

            return description;
        }

        private static List<YouTubeChapterItem> BuildChapters(ScriptStagePayload script, SceneLayoutStagePayload? layout)
        {
            if (layout?.EditDecisionList.Count > 0)
            {
                var chapterItems = layout.EditDecisionList
                    .Where(x => !string.IsNullOrWhiteSpace(x.ChapterTitle))
                    .GroupBy(x => x.ChapterTitle.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.OrderBy(x => x.StartTime).First())
                    .OrderBy(x => x.StartTime)
                    .Take(12)
                    .Select((item, index) => new YouTubeChapterItem
                    {
                        StartSec = index == 0 ? 0 : Math.Max(0, item.StartTime),
                        Timestamp = FormatTimestamp(index == 0 ? 0 : Math.Max(0, item.StartTime)),
                        Title = item.ChapterTitle
                    })
                    .ToList();

                if (chapterItems.Count > 0 && chapterItems[0].Timestamp != "00:00")
                {
                    chapterItems[0].StartSec = 0;
                    chapterItems[0].Timestamp = "00:00";
                }

                return chapterItems;
            }

            var scenes = script.Scenes.OrderBy(x => x.SceneNumber).ToList();
            var total = 0.0;
            var chapters = new List<YouTubeChapterItem>();

            foreach (var scene in scenes)
            {
                if (!string.IsNullOrWhiteSpace(scene.ChapterTitle)
                    && !chapters.Any(x => string.Equals(x.Title, scene.ChapterTitle.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    chapters.Add(new YouTubeChapterItem
                    {
                        StartSec = chapters.Count == 0 ? 0 : total,
                        Timestamp = FormatTimestamp(chapters.Count == 0 ? 0 : total),
                        Title = scene.ChapterTitle.Trim()
                    });
                }

                total += Math.Max(4, scene.EstimatedDuration);
            }

            if (chapters.Count == 0)
            {
                chapters.Add(new YouTubeChapterItem { StartSec = 0, Timestamp = "00:00", Title = "Intro" });
            }

            return chapters.Take(12).ToList();
        }

        private static List<string> NormalizeTags(List<string>? scriptTags, string title, ProductionBrief? brief)
        {
            var tags = new List<string>();
            if (scriptTags != null) tags.AddRange(scriptTags);
            tags.AddRange(title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 3).Take(8));
            if (!string.IsNullOrWhiteSpace(brief?.Audience)) tags.Add(brief.Audience);

            return tags
                .Select(x => x.Trim().TrimStart('#'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToList();
        }

        private static string BuildPinnedComment(string title)
            => $"What part of \"{title}\" surprised you the most?";

        private static string FormatTimestamp(double seconds)
        {
            var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
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
