namespace Application.Services
{
    public static class ScriptPromptDefaults
    {
        public const string DefaultSystemInstruction = """
You are a senior YouTube long-form scriptwriter and scene director for educational infotainment videos.
You write clean narration for TTS, not screenplay directions.
You also provide concise scene-direction intent so downstream Storyboard, Image, EditPlan and Render stages can make human-like editing decisions.
Follow the backend-provided JSON contract exactly. Do not invent unsupported top-level sections.
Keep narration coherent across scenes; every scene must feel like part of one continuous essay.
""";

        public const string DefaultPromptTemplate = """
Create a long-form YouTube video script about: {Topic}

Language: {Language}
Tone: {Tone}
Target duration: {Duration} seconds

Video format:
- Long-form YouTube video.
- 16:9 horizontal video.
- The final video should feel deliberately edited, not like disconnected captions.
- The visual style should follow the Topic document and production brief.

Structure:
1. Strong hook in the first 20 seconds.
2. Short intro that promises the payoff.
3. 4 to 7 clear chapters/sections.
4. Smooth transitions between chapters.
5. Short recap near the end.
6. Outro with a natural call to action.

Scene rules:
- Create enough narration scenes for long-form pacing, usually 45 to 80 scenes for an 8-12 minute video.
- Prefer 6 to 14 seconds per script scene. Important ideas can be slightly longer.
- audioText should be natural narration, ready for TTS.
- visualPrompt should describe one image-generation-ready 16:9 visual for the scene.
- Avoid on-screen text requirements inside visualPrompt.
- Do not ask the image generator to render labels, subtitles, UI, logos, watermarks, or written paragraphs.
- Use sceneRole, scenePurpose, viewerQuestion, emotionalBeat, visualType, cameraPlan, overlayText, sfxCue, transitionIntent and chapterTitle to explain how the scene should be edited.
- Vary visualType and cameraPlan across consecutive scenes.
- Use a mix of cinematic_image, broll, map, timeline, diagram, quote_card, comparison and text_card when they fit the idea.
- overlayText should be rare and shorter than 6 words.
- sfxCue should be sparse and meaningful.
- Let Storyboard/EditPlan handle multiple visual beats inside a scene; do not force one script scene for every 4 seconds.
- Keep the total estimated duration close to {Duration} seconds.
""";

        public static string ResolvePromptTemplate(string? promptTemplate)
            => string.IsNullOrWhiteSpace(promptTemplate)
                ? DefaultPromptTemplate
                : promptTemplate.Trim();

        public static string ResolveSystemInstruction(string? systemInstruction)
            => string.IsNullOrWhiteSpace(systemInstruction)
                ? DefaultSystemInstruction
                : systemInstruction.Trim();

        public static string? CleanOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static bool IsDefaultPromptTemplate(string? promptTemplate)
            => MatchesDefault(promptTemplate, DefaultPromptTemplate);

        public static bool IsDefaultSystemInstruction(string? systemInstruction)
            => MatchesDefault(systemInstruction, DefaultSystemInstruction);

        private static bool MatchesDefault(string? value, string defaultValue)
            => string.Equals(Normalize(value), Normalize(defaultValue), StringComparison.Ordinal);

        private static string Normalize(string? value)
            => (value ?? string.Empty).Replace("\r\n", "\n").Trim();
    }
}
