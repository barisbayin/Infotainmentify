using Application.Contracts.Auth;
using FluentValidation;

namespace Application.Validators
{
    public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Login).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        }
    }
}
