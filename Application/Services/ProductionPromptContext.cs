using Application.Contracts.ConceptProfiles;
using Application.Models;
using Core.Entity.Pipeline;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    public static class ProductionPromptContext
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static ConceptProfileDto? GetConceptProfile(ContentPipelineRun run)
        {
            if (string.IsNullOrWhiteSpace(run.InputConceptProfileJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ConceptProfileDto>(run.InputConceptProfileJson, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static string BuildTopicContextBlock(ConceptProfileDto? profile)
        {
            if (profile == null) return "";

            var sb = StartBlock("CONCEPT PROFILE", "Use these as durable concept defaults. A more specific production brief overrides them.");
            Append(sb, "Concept", profile.ConceptName);
            Append(sb, "Production profile", profile.ProductionProfile);
            Append(sb, "Default platform", profile.DefaultPlatform);
            Append(sb, "Default language", profile.DefaultLanguage);
            Append(sb, "Target audience", profile.Audience);
            Append(sb, "Channel promise", profile.ChannelPromise);
            Append(sb, "Tone", profile.Tone);
            Append(sb, "Content rules", profile.ContentRules);
            Append(sb, "Visual style", profile.VisualStyleName);
            Append(sb, "Default duration seconds", profile.DefaultDurationSec?.ToString());
            return sb.ToString().Trim();
        }

        public static string BuildScriptContextBlock(ConceptProfileDto? profile)
        {
            if (profile == null) return "";

            var sb = StartBlock("CONCEPT SCRIPT CONTEXT", "Use this profile to keep the script, chapters, scene intent and YouTube packaging consistent.");
            Append(sb, "Target audience", profile.Audience);
            Append(sb, "Channel promise", profile.ChannelPromise);
            Append(sb, "Tone", profile.Tone);
            Append(sb, "Content rules", profile.ContentRules);
            Append(sb, "Default platform", profile.DefaultPlatform);
            Append(sb, "Visual style", profile.VisualStyleName);
            Append(sb, "Text policy", profile.TextPolicy);
            Append(sb, "Default duration seconds", profile.DefaultDurationSec?.ToString());
            return sb.ToString().Trim();
        }

        public static string BuildCreativeDirectorContextBlock(ConceptProfileDto? profile)
        {
            if (profile == null) return "";

            var sb = StartBlock("CONCEPT DIRECTION CONTEXT", "This is the durable channel/concept identity. Turn it into retention, pacing and visual strategy.");
            Append(sb, "Audience", profile.Audience);
            Append(sb, "Channel promise", profile.ChannelPromise);
            Append(sb, "Tone", profile.Tone);
            Append(sb, "Content rules", profile.ContentRules);
            Append(sb, "Visual style name", profile.VisualStyleName);
            Append(sb, "Visual style bible", profile.VisualStyleBible);
            Append(sb, "Character bible", profile.CharacterBible);
            Append(sb, "Text policy", profile.TextPolicy);
            return sb.ToString().Trim();
        }

        public static string BuildStoryboardContextBlock(ConceptProfileDto? profile)
        {
            if (profile == null) return "";

            var sb = StartBlock("CONCEPT VISUAL IDENTITY", "Apply these rules before inventing any new style. Keep variety inside this identity.");
            Append(sb, "Visual style name", profile.VisualStyleName);
            Append(sb, "Visual style bible", profile.VisualStyleBible);
            Append(sb, "Character bible", profile.CharacterBible);
            Append(sb, "Text policy", profile.TextPolicy);
            Append(sb, "Content rules", profile.ContentRules);
            return sb.ToString().Trim();
        }

        public static string BuildImageContextBlock(ConceptProfileDto? profile)
        {
            if (profile == null) return "";

            var sb = StartBlock("CONCEPT IMAGE RULES", "These are hard visual identity rules for image generation.");
            Append(sb, "Visual style name", profile.VisualStyleName);
            Append(sb, "Visual style bible", profile.VisualStyleBible);
            Append(sb, "Character bible", profile.CharacterBible);
            Append(sb, "Text policy", profile.TextPolicy);
            Append(sb, "Content rules", profile.ContentRules);
            return sb.ToString().Trim();
        }

        public static string ApplyPlaceholders(string template, ConceptProfileDto? profile)
        {
            if (string.IsNullOrEmpty(template) || profile == null)
                return template;

            return template
                .Replace("{ConceptProfile}", BuildTopicContextBlock(profile))
                .Replace("{ConceptName}", profile.ConceptName ?? "")
                .Replace("{ProductionProfile}", profile.ProductionProfile ?? "")
                .Replace("{DefaultPlatform}", profile.DefaultPlatform ?? "")
                .Replace("{DefaultLanguage}", profile.DefaultLanguage ?? "")
                .Replace("{ChannelPromise}", profile.ChannelPromise ?? "")
                .Replace("{ConceptAudience}", profile.Audience ?? "")
                .Replace("{ConceptTone}", profile.Tone ?? "")
                .Replace("{VisualStyle}", profile.VisualStyleName ?? "")
                .Replace("{VisualStyleName}", profile.VisualStyleName ?? "")
                .Replace("{StyleBible}", profile.VisualStyleBible ?? "")
                .Replace("{VisualStyleBible}", profile.VisualStyleBible ?? "")
                .Replace("{CharacterBible}", profile.CharacterBible ?? "")
                .Replace("{TextPolicy}", profile.TextPolicy ?? "")
                .Replace("{ContentRules}", profile.ContentRules ?? "")
                .Replace("{DefaultDurationSec}", profile.DefaultDurationSec?.ToString() ?? "");
        }

        public static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";

        public static string ResolveLanguage(ConceptProfileDto? profile, string fallback)
            => FirstNonEmpty(profile?.DefaultLanguage, fallback);

        public static string ResolveTone(ConceptProfileDto? profile, string fallback)
            => FirstNonEmpty(profile?.Tone, fallback);

        private static StringBuilder StartBlock(string title, string instruction)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine(instruction);
            return sb;
        }

        private static void Append(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{label}: {value.Trim()}");
        }
    }
}
