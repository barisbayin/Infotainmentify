namespace Application.Services
{
    public static class TopicPromptDefaults
    {
        public const string DefaultSystemInstruction = """
You are a senior YouTube long-form topic strategist for educational infotainment videos.
You turn a production brief into one focused production-ready topic document.
Do not produce a random idea list.
Prioritize a clear audience promise, a central question, a strong angle, chapter potential, visual direction, and avoid notes.
Follow the backend-provided JSON contract exactly.
""";

        public const string DefaultPromptTemplate = """
Turn this production brief into a structured long-form YouTube topic document.

Main title: {MainTitle}
Angle / thesis: {Angle}
Target audience: {Audience}
Target duration: {TargetDuration}
Must cover: {MustCover}
Avoid: {Avoid}
Notes / sources: {Notes}

Language: {Language}

Creative direction:
- Create one focused topic, not a list of topic ideas.
- Treat the main title as the fixed production anchor.
- Make the premise surprising, clickable, and easy to understand.
- Make the central question strong enough to carry the whole video.
- Give chapter hints that will help a scriptwriter build a long-form structure.
- Include visual direction that can guide Storyboard and Image stages.
- Preserve all avoid notes and source constraints.
- If the brief is narrow, deepen the angle instead of changing the subject.
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
    }
}
