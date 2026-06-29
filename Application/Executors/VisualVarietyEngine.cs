using Application.Models;

namespace Application.Executors
{
    internal static class VisualVarietyEngine
    {
        private static readonly string[] BasePalette =
        {
            "cinematic_image",
            "broll",
            "diagram",
            "comparison",
            "timeline",
            "map",
            "quote_card",
            "text_card"
        };

        private static readonly string[] InfoPalette =
        {
            "diagram",
            "timeline",
            "comparison",
            "map"
        };

        public static VisualVarietySummary ApplyToScriptScenes(IList<ScriptSceneItem> scenes)
        {
            var ordered = scenes.OrderBy(x => x.SceneNumber).ToList();
            var summary = new VisualVarietySummary();

            if (ordered.Count == 0)
                return summary;

            var lastVisualType = "";
            var secondsSinceInfoVisual = 0.0;
            var isLongForm = ordered.Sum(x => Math.Max(4, x.EstimatedDuration)) >= 180 || ordered.Count >= 12;

            for (var index = 0; index < ordered.Count; index++)
            {
                var scene = ordered[index];
                var originalType = NormalizeVisualType(scene.VisualType);
                var visualType = originalType;
                var shouldForceInfoVisual = isLongForm
                    && !IsInfoVisual(visualType)
                    && (secondsSinceInfoVisual >= 42 || ((index + 1) % 7 == 0 && secondsSinceInfoVisual >= 24));

                if (shouldForceInfoVisual)
                {
                    visualType = PickInfoVisual(scene, index);
                    summary.InfoVisualInjections++;
                }
                else if (index > 0 && visualType == lastVisualType)
                {
                    visualType = PickAlternativeVisual(scene, index, lastVisualType);
                    summary.RepeatBreaks++;
                }

                if (visualType != originalType)
                {
                    summary.ChangedScenes++;
                    scene.CameraPlan = DefaultCameraPlan(visualType, scene.SceneNumber);
                }
                else if (string.IsNullOrWhiteSpace(scene.CameraPlan))
                {
                    scene.CameraPlan = DefaultCameraPlan(visualType, scene.SceneNumber);
                }

                scene.VisualType = visualType;
                scene.VisualVarietyRole = BuildVarietyRole(scene, index);
                scene.VisualVarietyReason = BuildVarietyReason(scene, originalType, shouldForceInfoVisual, index > 0 && originalType == lastVisualType);

                if (string.IsNullOrWhiteSpace(scene.OverlayText) && visualType is "quote_card" or "text_card" or "comparison")
                    scene.OverlayText = BuildOverlayText(scene);

                if (string.IsNullOrWhiteSpace(scene.SfxCue))
                    scene.SfxCue = scene.SceneNumber == 1 ? "low_boom" : "none";

                if (string.IsNullOrWhiteSpace(scene.TransitionIntent))
                    scene.TransitionIntent = visualType is "quote_card" or "text_card" ? "dip_black" : "cut";

                if (IsInfoVisual(visualType))
                {
                    secondsSinceInfoVisual = 0;
                    summary.InfoVisualCount++;
                }
                else
                {
                    secondsSinceInfoVisual += Math.Max(4, scene.EstimatedDuration);
                }

                summary.Register(visualType);
                lastVisualType = visualType;
            }

            if (isLongForm)
            {
                EnsureChapterInfoVisuals(ordered, summary);
            }

            return summary;
        }

        public static VisualQualitySignal ScorePrompt(
            ScriptSceneItem scene,
            StoryboardScenePlan? scenePlan,
            StoryboardVisualBeat beat,
            string finalPrompt)
        {
            var score = 58;
            var notes = new List<string>();

            if (!string.IsNullOrWhiteSpace(scene.VisualType))
            {
                score += 8;
                notes.Add($"visualType:{scene.VisualType}");
            }

            if (!string.IsNullOrWhiteSpace(scene.VisualVarietyRole))
            {
                score += 6;
                notes.Add($"role:{scene.VisualVarietyRole}");
            }

            if (!string.IsNullOrWhiteSpace(beat.Composition))
                score += 8;

            if (!string.IsNullOrWhiteSpace(beat.Lens))
                score += 4;

            if (!string.IsNullOrWhiteSpace(beat.Lighting))
                score += 4;

            if (!string.IsNullOrWhiteSpace(scenePlan?.ContinuityAnchor) || !string.IsNullOrWhiteSpace(beat.ContinuityNotes))
                score += 5;

            if (IsInfoVisual(scene.VisualType))
                score += 5;

            if (string.IsNullOrWhiteSpace(finalPrompt) || finalPrompt.Length < 120)
            {
                score -= 12;
                notes.Add("prompt_short");
            }

            if (WordCount(scene.OverlayText) > 6)
            {
                score -= 8;
                notes.Add("overlay_long");
            }

            if (finalPrompt.Contains("generic stock", StringComparison.OrdinalIgnoreCase))
            {
                score -= 8;
                notes.Add("generic_risk");
            }

            return new VisualQualitySignal
            {
                Score = Math.Clamp(score, 0, 100),
                Notes = string.Join(", ", notes.Distinct())
            };
        }

        public static string NormalizeVisualType(string value)
        {
            var token = NormalizeToken(value, "cinematic_image");
            return token switch
            {
                "image" or "cinematic" or "cinematic_image" => "cinematic_image",
                "b_roll" or "broll" => "broll",
                "map" => "map",
                "timeline" => "timeline",
                "diagram" => "diagram",
                "quote" or "quote_card" => "quote_card",
                "comparison" or "comparison_card" => "comparison",
                "text" or "text_card" => "text_card",
                _ => "cinematic_image"
            };
        }

        public static string DefaultCameraPlan(string visualType, int sceneNumber)
        {
            return NormalizeVisualType(visualType) switch
            {
                "timeline" => "clean timeline composition, static hold, readable visual hierarchy",
                "map" => "top-down map-like composition, slow push-in toward the key region",
                "diagram" => "diagram-like composition with strong negative space, static hold",
                "quote_card" => "close-up symbolic background with centered negative space for overlay",
                "comparison" => "split composition with two contrasting visual zones",
                "text_card" => "minimal background with clean negative space for overlay",
                "broll" => "detail shot with shallow depth of field and slow lateral motion",
                _ => sceneNumber == 1
                    ? "wide cinematic establishing shot with slow push-in"
                    : "medium cinematic shot with motivated camera movement"
            };
        }

        private static bool IsInfoVisual(string value)
        {
            var token = NormalizeVisualType(value);
            return token is "map" or "timeline" or "diagram" or "comparison";
        }

        private static string PickInfoVisual(ScriptSceneItem scene, int index)
        {
            var role = NormalizeToken(scene.SceneRole, "");
            if (role.Contains("context") || role.Contains("setup"))
                return "timeline";
            if (role.Contains("proof") || role.Contains("example"))
                return "diagram";
            if (role.Contains("reveal") || role.Contains("contrast"))
                return "comparison";

            return InfoPalette[index % InfoPalette.Length];
        }

        private static void EnsureChapterInfoVisuals(List<ScriptSceneItem> ordered, VisualVarietySummary summary)
        {
            var groups = ordered
                .Select((scene, index) => new { Scene = scene, Index = index, Key = BuildChapterKey(scene, index) })
                .GroupBy(x => x.Key)
                .Where(group => group.Count() >= 3)
                .ToList();

            foreach (var group in groups)
            {
                if (group.Any(x => IsInfoVisual(x.Scene.VisualType)))
                    continue;

                var candidate = group
                    .OrderBy(x => IsAvoidedInfoVisualRole(x.Scene) ? 1 : 0)
                    .ThenBy(x => Math.Abs(x.Index - group.Average(y => y.Index)))
                    .First();

                var originalType = NormalizeVisualType(candidate.Scene.VisualType);
                var infoType = PickInfoVisual(candidate.Scene, candidate.Index);

                candidate.Scene.VisualType = infoType;
                candidate.Scene.CameraPlan = DefaultCameraPlan(infoType, candidate.Scene.SceneNumber);
                candidate.Scene.VisualVarietyRole = BuildVarietyRole(candidate.Scene, candidate.Index);
                candidate.Scene.VisualVarietyReason = "Bolum basina en az bir bilgi gorseli hedefi icin eklendi.";

                if (string.IsNullOrWhiteSpace(candidate.Scene.TransitionIntent))
                    candidate.Scene.TransitionIntent = "cut";

                summary.ChangedScenes++;
                summary.InfoVisualInjections++;
                summary.InfoVisualCount++;
                summary.Register(infoType);

                if (originalType == infoType)
                    summary.RepeatBreaks++;
            }
        }

        private static string BuildChapterKey(ScriptSceneItem scene, int index)
        {
            if (!string.IsNullOrWhiteSpace(scene.ChapterTitle))
                return NormalizeToken(scene.ChapterTitle, $"bucket_{index / 8}");

            return $"bucket_{index / 8}";
        }

        private static bool IsAvoidedInfoVisualRole(ScriptSceneItem scene)
        {
            var role = NormalizeToken(scene.SceneRole, "");
            return scene.SceneNumber == 1 || role.Contains("hook") || role.Contains("outro") || role.Contains("recap");
        }

        private static string PickAlternativeVisual(ScriptSceneItem scene, int index, string previousType)
        {
            var preferred = PickBySceneRole(scene, index);
            if (preferred != previousType)
                return preferred;

            return BasePalette.First(x => x != previousType);
        }

        private static string PickBySceneRole(ScriptSceneItem scene, int index)
        {
            var role = NormalizeToken(scene.SceneRole, "");
            if (role.Contains("proof") || role.Contains("example"))
                return index % 2 == 0 ? "diagram" : "comparison";
            if (role.Contains("context") || role.Contains("setup"))
                return index % 2 == 0 ? "timeline" : "map";
            if (role.Contains("recap"))
                return "text_card";
            if (role.Contains("reveal"))
                return "comparison";
            if (role.Contains("hook"))
                return "cinematic_image";

            return BasePalette[index % BasePalette.Length];
        }

        private static string BuildVarietyRole(ScriptSceneItem scene, int index)
        {
            var visualType = NormalizeVisualType(scene.VisualType);
            if (scene.SceneNumber == 1)
                return "hook_anchor";

            return visualType switch
            {
                "map" or "timeline" or "diagram" => "info_visual",
                "comparison" => "contrast_visual",
                "quote_card" or "text_card" => "emphasis_card",
                "broll" => "texture_refresh",
                _ => index % 3 == 0 ? "narrative_anchor" : "cinematic_refresh"
            };
        }

        private static string BuildVarietyReason(ScriptSceneItem scene, string originalType, bool forcedInfo, bool brokeRepeat)
        {
            if (forcedInfo)
                return "Long-form tempo icin bilgi gorseli eklendi.";
            if (brokeRepeat)
                return $"Ust uste {originalType} tekrarini kirmak icin tip degistirildi.";
            if (!string.IsNullOrWhiteSpace(scene.ScenePurpose))
                return scene.ScenePurpose;
            return "Sahneye yeni bir gorsel ritim vermek icin secildi.";
        }

        private static string BuildOverlayText(ScriptSceneItem scene)
        {
            var source = FirstNonEmpty(scene.ChapterTitle, scene.ViewerQuestion, scene.SceneRole);
            if (string.IsNullOrWhiteSpace(source))
                return "";

            var words = source
                .Replace("?", "")
                .Replace("!", "")
                .Replace(".", "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(6);

            return string.Join(" ", words);
        }

        private static int WordCount(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? 0
                : value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "";

        private static string NormalizeToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }

    internal sealed class VisualVarietySummary
    {
        private readonly Dictionary<string, int> _distribution = new(StringComparer.OrdinalIgnoreCase);

        public int ChangedScenes { get; set; }
        public int RepeatBreaks { get; set; }
        public int InfoVisualInjections { get; set; }
        public int InfoVisualCount { get; set; }

        public string DistributionText => string.Join(", ", _distribution.OrderByDescending(x => x.Value).Select(x => $"{x.Key}:{x.Value}"));

        public void Register(string visualType)
        {
            if (!_distribution.TryAdd(visualType, 1))
                _distribution[visualType]++;
        }
    }

    internal sealed class VisualQualitySignal
    {
        public int Score { get; set; }
        public string Notes { get; set; } = "";
    }
}
