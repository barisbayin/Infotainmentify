namespace Application.Services
{
    public static class ImagePromptDefaults
    {
        public const string DefaultPromptTemplate = """
{SceneDescription}

Create a 16:9 YouTube long-form visual.
Concept visual style: {VisualStyle}
Style bible: {StyleBible}
Character continuity: {CharacterBible}
Text policy: {TextPolicy}
Provider style hint: {ArtStyle}

Requirements:
- clear subject, readable composition, strong focal idea
- keep visual variety while staying inside the concept identity
- avoid random text, logos, watermark, UI, and unreadable labels
- suitable for a deliberate long-form edit, not a generic stock image
""";

        public static string ResolvePromptTemplate(string? promptTemplate)
            => string.IsNullOrWhiteSpace(promptTemplate)
                ? DefaultPromptTemplate
                : promptTemplate.Trim();

        public static string? CleanOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static bool IsDefaultPromptTemplate(string? promptTemplate)
            => string.Equals(Normalize(promptTemplate), Normalize(DefaultPromptTemplate), StringComparison.Ordinal);

        private static string Normalize(string? value)
            => (value ?? string.Empty).Replace("\r\n", "\n").Trim();
    }
}
