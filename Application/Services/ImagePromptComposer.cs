using Application.Contracts.ConceptProfiles;
using Application.Models;
using Core.Entity.Presets;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services
{
    public static class ImagePromptComposer
    {
        private const int MaxVisualBeatsPerScene = 4;

        private const string TextHandlingRule =
            "Text handling rule: readable text is allowed only when the visual prompt explicitly asks for a specific phrase, title, chapter card, sign, or speech bubble. If text is requested, render exactly that short text cleanly as part of the image. Otherwise avoid random captions, labels, UI, logos, watermarks, or stray letters.";

        public static IEnumerable<StoryboardVisualBeat> GetVisualBeats(
            ScriptSceneItem scene,
            StoryboardScenePlan? scenePlan,
            string? styleBible,
            ConceptProfileDto? conceptProfile = null)
        {
            var resolvedStyleBible = ProductionPromptContext.FirstNonEmpty(
                styleBible,
                conceptProfile?.VisualStyleBible,
                conceptProfile?.VisualStyleName);

            var desiredBeatCount = EstimateVisualBeatCount(scene);
            var beats = scenePlan?.VisualBeats != null && scenePlan.VisualBeats.Count > 0
                ? scenePlan.VisualBeats
                    .OrderBy(x => x.BeatIndex <= 0 ? 1 : x.BeatIndex)
                    .Take(MaxVisualBeatsPerScene)
                    .Select(CloneBeat)
                    .ToList()
                : new List<StoryboardVisualBeat>
                {
                    BuildFallbackBeat(scene, resolvedStyleBible, 1)
                };

            ExpandBeats(scene, beats, desiredBeatCount, resolvedStyleBible);
            NormalizeBeats(scene, beats);
            return beats;
        }

        public static string BuildBeatPrompt(
            ImagePreset preset,
            ScriptSceneItem scene,
            StoryboardScenePlan? scenePlan,
            StoryboardStagePayload? storyboard,
            StoryboardVisualBeat beat,
            ConceptProfileDto? conceptProfile = null)
        {
            var artStyle = ProductionPromptContext.FirstNonEmpty(
                preset.ArtStyle,
                conceptProfile?.VisualStyleName,
                "cinematic");
            var beatPrompt = string.IsNullOrWhiteSpace(beat.VisualPrompt)
                ? scene.VisualPrompt
                : beat.VisualPrompt;

            var directorContext = string.Join(" ", new[]
            {
                $"Shot type: {beat.ShotType}. Camera motion feeling: {beat.CameraMotion}.",
                $"Subject: {beat.Subject}. Composition: {beat.Composition}. Lens: {beat.Lens}. Lighting: {beat.Lighting}.",
                $"Scene tone: {scenePlan?.EmotionalTone ?? scene.EmotionalBeat ?? "curious"}. Scene type: {scenePlan?.SceneType ?? scene.SceneRole ?? "explanation"}.",
                $"Scene purpose: {scenePlan?.ScenePurpose ?? scene.ScenePurpose ?? ""}. Retention goal: {scenePlan?.RetentionGoal ?? scene.ViewerQuestion ?? ""}.",
                $"Visual type: {scene.VisualType}. Camera plan: {scene.CameraPlan}.",
                $"Visual variety role: {scene.VisualVarietyRole}. Variety reason: {scene.VisualVarietyReason}.",
                $"Continuity anchor: {scenePlan?.ContinuityAnchor ?? beat.ContinuityNotes}.",
                $"Visual continuity: {storyboard?.VisualContinuityBible ?? storyboard?.StyleBible ?? artStyle}.",
                $"Color palette: {storyboard?.ColorPalette ?? beat.ColorNotes}. Camera language: {storyboard?.CameraLanguage ?? ""}.",
                $"Lighting style: {storyboard?.LightingStyle ?? beat.Lighting}. Beat color notes: {beat.ColorNotes}.",
                ProductionPromptContext.BuildImageContextBlock(conceptProfile),
                $"Avoid: {storyboard?.NegativeVisualRules ?? ""}, {beat.NegativePrompt}. {TextHandlingRule}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var sceneDescription = $"{beatPrompt}. {directorContext}";

            var finalPrompt = ImagePromptDefaults.ResolvePromptTemplate(preset.PromptTemplate)
                .Replace("{SceneDescription}", sceneDescription)
                .Replace("{ArtStyle}", artStyle)
                .Trim();
            finalPrompt = ProductionPromptContext.ApplyPlaceholders(finalPrompt, conceptProfile);

            var composed = string.IsNullOrWhiteSpace(finalPrompt)
                ? $"{sceneDescription}, {artStyle}"
                : finalPrompt;

            return EnsureTextHandlingRule(composed);
        }

        public static string BuildNegativePrompt(
            ImagePreset preset,
            StoryboardStagePayload? storyboard,
            StoryboardVisualBeat? beat = null,
            ConceptProfileDto? conceptProfile = null)
        {
            var parts = new[]
            {
                preset.NegativePrompt,
                storyboard?.NegativeVisualRules,
                beat?.NegativePrompt,
                BuildConceptNegativePrompt(conceptProfile),
                UnwantedTextNegativePrompt,
                "generic stock photo, distorted hands, distorted faces, low quality"
            };

            return DistinctParts(parts);
        }

        public static string StrengthenNegativePrompt(string? negativePrompt)
        {
            return DistinctParts(new[]
            {
                negativePrompt,
                UnwantedTextNegativePrompt,
                "generic stock photo, distorted hands, distorted faces, low quality"
            });
        }

        public static string EnsureTextHandlingRule(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return TextHandlingRule;

            if (prompt.Contains(TextHandlingRule, StringComparison.OrdinalIgnoreCase))
                return prompt;

            return $"{prompt.Trim()}\n\n{TextHandlingRule}";
        }

        private static string UnwantedTextNegativePrompt =>
            "random text, gibberish text, misspelled text, extra letters, UI text, logo, watermark";

        private static string? BuildConceptNegativePrompt(ConceptProfileDto? profile)
        {
            if (profile == null) return null;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(profile.TextPolicy))
                parts.Add("random or unrequested text, gibberish text, misspelled text, stray letters");

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        private static string DistinctParts(IEnumerable<string?> parts)
        {
            return string.Join(", ",
                parts
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .SelectMany(x => x!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static int EstimateVisualBeatCount(ScriptSceneItem scene)
        {
            var duration = Math.Max(scene.EstimatedDuration, EstimateDurationFromNarration(scene.AudioText));

            if (duration >= 24) return 4;
            if (duration >= 16) return 3;
            if (duration >= 8) return 2;
            return 1;
        }

        private static int EstimateDurationFromNarration(string? audioText)
        {
            if (string.IsNullOrWhiteSpace(audioText)) return 0;

            var wordCount = audioText
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Length;

            return (int)Math.Ceiling(wordCount * 60.0 / 155.0);
        }

        private static void ExpandBeats(
            ScriptSceneItem scene,
            List<StoryboardVisualBeat> beats,
            int desiredBeatCount,
            string? styleBible)
        {
            var targetCount = Math.Clamp(desiredBeatCount, 1, MaxVisualBeatsPerScene);
            if (beats.Count == 0)
                beats.Add(BuildFallbackBeat(scene, styleBible, 1));

            while (beats.Count < targetCount)
            {
                beats.Add(BuildSupplementalBeat(scene, beats[0], beats.Count + 1, styleBible));
            }
        }

        private static void NormalizeBeats(ScriptSceneItem scene, List<StoryboardVisualBeat> beats)
        {
            for (var i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                beat.BeatIndex = i + 1;
                beat.BeatRole = string.IsNullOrWhiteSpace(beat.BeatRole)
                    ? (i == 0 ? FirstNonEmpty(scene.VisualVarietyRole, "primary") : PickRole(i + 1))
                    : beat.BeatRole;
                beat.ShotType = string.IsNullOrWhiteSpace(beat.ShotType) ? PickShotType(scene.VisualType, i + 1) : beat.ShotType;
                beat.CameraMotion = string.IsNullOrWhiteSpace(beat.CameraMotion) ? PickCameraMotion(scene.CameraPlan, i + 1) : beat.CameraMotion;
                beat.Subject = string.IsNullOrWhiteSpace(beat.Subject) ? "main narration idea" : beat.Subject;
                beat.Composition = string.IsNullOrWhiteSpace(beat.Composition) ? PickComposition(scene.VisualType, i + 1) : beat.Composition;
                beat.Lens = string.IsNullOrWhiteSpace(beat.Lens) ? (i == 0 ? "35mm documentary lens" : "50mm detail lens") : beat.Lens;
                beat.Lighting = string.IsNullOrWhiteSpace(beat.Lighting) ? "motivated soft cinematic light" : beat.Lighting;
                beat.ColorNotes = string.IsNullOrWhiteSpace(beat.ColorNotes) ? "match the video palette" : beat.ColorNotes;
                beat.ContinuityNotes = string.IsNullOrWhiteSpace(beat.ContinuityNotes)
                    ? FirstNonEmpty(scene.VisualVarietyReason, "keep visual continuity while changing scale or angle")
                    : beat.ContinuityNotes;
                beat.NegativePrompt = string.IsNullOrWhiteSpace(beat.NegativePrompt) ? UnwantedTextNegativePrompt : beat.NegativePrompt;
                beat.CutIntent = string.IsNullOrWhiteSpace(beat.CutIntent)
                    ? (i == 0 ? FirstNonEmpty(scene.ScenePurpose, "establish the scene idea") : "refresh attention with a new visual angle inside the same narration")
                    : beat.CutIntent;
                beat.VisualPrompt = string.IsNullOrWhiteSpace(beat.VisualPrompt) ? scene.VisualPrompt : beat.VisualPrompt;
                beat.DurationWeight = Math.Clamp(beat.DurationWeight <= 0 ? 1.0 : beat.DurationWeight, 0.25, 4.0);
            }
        }

        private static StoryboardVisualBeat CloneBeat(StoryboardVisualBeat beat) =>
            new()
            {
                BeatIndex = beat.BeatIndex,
                BeatRole = beat.BeatRole,
                ShotType = beat.ShotType,
                CameraMotion = beat.CameraMotion,
                Subject = beat.Subject,
                Composition = beat.Composition,
                Lens = beat.Lens,
                Lighting = beat.Lighting,
                ColorNotes = beat.ColorNotes,
                ContinuityNotes = beat.ContinuityNotes,
                NegativePrompt = beat.NegativePrompt,
                CutIntent = beat.CutIntent,
                VisualPrompt = beat.VisualPrompt,
                OnScreenText = beat.OnScreenText,
                DurationWeight = beat.DurationWeight
            };

        private static StoryboardVisualBeat BuildFallbackBeat(ScriptSceneItem scene, string? styleBible, int beatIndex)
        {
            var narrationFocus = ExtractNarrationFocus(scene.AudioText, beatIndex, Math.Max(beatIndex, EstimateVisualBeatCount(scene)));
            var visualPrompt = string.IsNullOrWhiteSpace(styleBible)
                ? scene.VisualPrompt
                : $"{scene.VisualPrompt}, {styleBible}";

            if (!string.IsNullOrWhiteSpace(narrationFocus))
                visualPrompt = $"{visualPrompt}. Narration focus for this visual beat: {narrationFocus}.";

            return new StoryboardVisualBeat
            {
                BeatIndex = beatIndex,
                BeatRole = beatIndex == 1 ? FirstNonEmpty(scene.VisualVarietyRole, "primary") : PickRole(beatIndex),
                ShotType = PickShotType(scene.VisualType, beatIndex),
                CameraMotion = PickCameraMotion(scene.CameraPlan, beatIndex),
                Subject = beatIndex == 1 ? FirstNonEmpty(narrationFocus, "main narration idea") : FirstNonEmpty(narrationFocus, "supporting visual evidence"),
                Composition = PickComposition(scene.VisualType, beatIndex),
                Lens = beatIndex == 1 ? "35mm documentary lens" : "50mm detail lens",
                Lighting = "motivated soft cinematic light",
                ColorNotes = "match the video palette",
                ContinuityNotes = FirstNonEmpty(scene.VisualVarietyReason, "keep visual continuity while changing scale or angle"),
                NegativePrompt = UnwantedTextNegativePrompt,
                CutIntent = beatIndex == 1 ? FirstNonEmpty(scene.ScenePurpose, "establish the scene idea") : "refresh attention with a new visual angle inside the same narration",
                VisualPrompt = visualPrompt,
                OnScreenText = beatIndex == 1 ? scene.OverlayText : "",
                DurationWeight = beatIndex == 1 ? 1.1 : 1.0
            };
        }

        private static StoryboardVisualBeat BuildSupplementalBeat(
            ScriptSceneItem scene,
            StoryboardVisualBeat baseBeat,
            int beatIndex,
            string? styleBible)
        {
            var desiredBeatCount = EstimateVisualBeatCount(scene);
            var narrationFocus = ExtractNarrationFocus(scene.AudioText, beatIndex, desiredBeatCount);
            var visualPrompt = FirstNonEmpty(baseBeat.VisualPrompt, scene.VisualPrompt);
            var supplement =
                $"Alternate visual beat {beatIndex} for the same scene narration. Narration focus: {FirstNonEmpty(narrationFocus, "the next meaningful idea inside the same narration")}. Use a {PickRole(beatIndex)} angle, {PickShotType(scene.VisualType, beatIndex)}, {PickComposition(scene.VisualType, beatIndex)}. Keep the same concept, characters and style, but make the image specifically support this narration focus. Do not drift into a new topic. Do not add text unless explicitly requested.";

            if (!string.IsNullOrWhiteSpace(styleBible))
                supplement += $" Style continuity: {styleBible}.";

            return new StoryboardVisualBeat
            {
                BeatIndex = beatIndex,
                BeatRole = PickRole(beatIndex),
                ShotType = PickShotType(scene.VisualType, beatIndex),
                CameraMotion = PickCameraMotion(scene.CameraPlan, beatIndex),
                Subject = FirstNonEmpty(baseBeat.Subject, "supporting visual evidence"),
                Composition = PickComposition(scene.VisualType, beatIndex),
                Lens = beatIndex % 2 == 0 ? "50mm detail lens" : "35mm documentary lens",
                Lighting = FirstNonEmpty(baseBeat.Lighting, "motivated soft cinematic light"),
                ColorNotes = FirstNonEmpty(baseBeat.ColorNotes, "match the video palette"),
                ContinuityNotes = FirstNonEmpty(baseBeat.ContinuityNotes, scene.VisualVarietyReason, "same scene idea, fresh scale or angle"),
                NegativePrompt = FirstNonEmpty(baseBeat.NegativePrompt, UnwantedTextNegativePrompt),
                CutIntent = "refresh attention and match a new idea beat inside the narration",
                VisualPrompt = $"{visualPrompt}. {supplement}",
                OnScreenText = "",
                DurationWeight = 1.0
            };
        }

        private static string PickRole(int beatIndex) =>
            beatIndex switch
            {
                2 => "detail",
                3 => "contrast",
                4 => "reaction",
                _ => "broll"
            };

        private static string PickShotType(string visualType, int beatIndex)
        {
            if (beatIndex == 1) return DefaultShotType(visualType);

            return beatIndex switch
            {
                2 => "close-up detail",
                3 => "wide contextual composition",
                4 => "reaction or symbolic insert",
                _ => "supporting b-roll composition"
            };
        }

        private static string PickComposition(string visualType, int beatIndex)
        {
            if (beatIndex == 1) return DefaultComposition(visualType);

            return beatIndex switch
            {
                2 => "tight off-center detail composition",
                3 => "wider context composition with clear spatial contrast",
                4 => "expressive reaction or symbolic insert composition",
                _ => "clean supporting composition"
            };
        }

        private static string PickCameraMotion(string? cameraPlan, int beatIndex)
        {
            if (beatIndex == 1) return MapCameraPlanToMotion(cameraPlan);

            return beatIndex switch
            {
                2 => "pan_left",
                3 => "slow_pull_out",
                4 => "pan_right",
                _ => "slow_push_in"
            };
        }

        private static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";

        private static string ExtractNarrationFocus(string? audioText, int beatIndex, int beatCount)
        {
            if (string.IsNullOrWhiteSpace(audioText)) return "";

            var parts = audioText
                .Replace("*", "")
                .Split(new[] { ". ", "? ", "! ", "; ", ": ", ", but ", ", then ", ", and " }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim(' ', '.', '?', '!', ';', ':', ','))
                .Where(x => x.Length >= 12)
                .ToList();

            if (parts.Count == 0)
                return audioText.Length <= 220 ? audioText.Trim() : audioText.Trim()[..220];

            var targetCount = Math.Clamp(beatCount, 1, MaxVisualBeatsPerScene);
            var targetIndex = Math.Clamp(beatIndex - 1, 0, targetCount - 1);
            var partIndex = Math.Clamp((int)Math.Round(targetIndex * (parts.Count - 1) / Math.Max(1.0, targetCount - 1.0)), 0, parts.Count - 1);
            var focus = parts[partIndex];

            return focus.Length <= 220 ? focus : focus[..220];
        }

        private static string DefaultShotType(string visualType)
        {
            var token = NormalizeToken(visualType);
            return token switch
            {
                "timeline" => "timeline composition",
                "map" => "top-down map-like composition",
                "diagram" => "diagram-like composition",
                "quote_card" => "quote card background",
                "comparison" => "split comparison composition",
                "text_card" => "minimal chapter-divider background with negative space",
                "broll" or "b_roll" => "close-up b-roll detail",
                _ => "cinematic medium shot"
            };
        }

        private static string DefaultComposition(string visualType)
        {
            var token = NormalizeToken(visualType);
            return token switch
            {
                "timeline" => "clean timeline layout with clear visual hierarchy",
                "map" => "top-down map-like composition with strong focal area",
                "diagram" => "diagram-like composition with readable negative space",
                "quote_card" => "symbolic background with centered negative space",
                "comparison" => "split composition with contrasting visual zones",
                "text_card" => "minimal chapter-card-safe composition with clean space for intentional title text when requested",
                "broll" or "b_roll" => "off-center detail composition",
                _ => "clean subject-led cinematic composition"
            };
        }

        private static string MapCameraPlanToMotion(string? cameraPlan)
        {
            var token = NormalizeToken(cameraPlan);
            if (token.Contains("pull") || token.Contains("zoom_out"))
                return "slow_pull_out";
            if (token.Contains("pan_left"))
                return "pan_left";
            if (token.Contains("pan_right"))
                return "pan_right";
            if (token.Contains("static") || token.Contains("hold"))
                return "static_hold";
            return "slow_push_in";
        }

        private static string NormalizeToken(string? value) =>
            (value ?? "").Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }
}
