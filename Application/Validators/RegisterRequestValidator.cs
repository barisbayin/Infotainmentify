using Application.Contracts.Auth;
using FluentValidation;

namespace Application.Validators
{
    public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(128);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        }
    }
}
