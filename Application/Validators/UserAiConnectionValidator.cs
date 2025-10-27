using Application.Contracts.Ai;
using Core.Enums;
using FluentValidation;

namespace Application.Validators
{
    public class UserAiConnectionValidator : AbstractValidator<UserAiConnectionDetailDto>
    {
        public UserAiConnectionValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Credentials).NotNull();

            When(x => x.AuthType == AiAuthType.ApiKey, () =>
            {
                RuleFor(x => x.Credentials.ContainsKey("apiKey"))
                    .Equal(true).WithMessage("apiKey is required.");
            });

            When(x => x.AuthType == AiAuthType.ApiKeySecret, () =>
            {
                RuleFor(x => x.Credentials.ContainsKey("apiKey")).Equal(true);
                RuleFor(x => x.Credentials.ContainsKey("apiSecret")).Equal(true);
            });

            // OAuth init: kaydı boş credential ile oluşturup "Connect" akışıyla tamamlayabiliriz.
            // Eğer doğrudan token verilecekse:
            // RuleFor(x => x.Credentials.ContainsKey("accessToken")).Equal(true).When(x => x.AuthType == AiAuthType.OAuth2);
        }
    }
}
