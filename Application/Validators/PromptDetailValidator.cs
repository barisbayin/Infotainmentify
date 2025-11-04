using Application.Contracts.Prompts;
using FluentValidation;

namespace Application.Validators
{
    public sealed class PromptDetailValidator : AbstractValidator<PromptDetailDto>
    {
        public PromptDetailValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Category).MaximumLength(64);
            RuleFor(x => x.Language).MaximumLength(10);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Body).NotEmpty();
            RuleFor(x => x.SystemPrompt).MaximumLength(4000);
        }
    }
}
