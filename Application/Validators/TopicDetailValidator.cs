using Application.Contracts.Prompts;
using Application.Contracts.Topics;
using FluentValidation;

namespace Application.Validators
{
    public sealed class TopicDetailValidator : AbstractValidator<TopicDetailDto>
    {
        public TopicDetailValidator()
        {
            RuleFor(x => x.TopicCode)
                .NotEmpty().WithMessage("TopicCode zorunludur.")
                .MaximumLength(64);

            RuleFor(x => x.Category)
                .MaximumLength(64);

            RuleFor(x => x.PremiseTr)
                .MaximumLength(2000);

            RuleFor(x => x.Premise)
                .MaximumLength(2000);

            RuleFor(x => x.Tone)
                .MaximumLength(64);

            RuleFor(x => x.PotentialVisual)
                .MaximumLength(256);

            RuleFor(x => x.TopicJson)
                .Must(BeValidJson)
                .When(x => !string.IsNullOrWhiteSpace(x.TopicJson))
                .WithMessage("TopicJson geçerli bir JSON olmalıdır.");
        }

        private static bool BeValidJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return true;
            try
            {
                _ = System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
