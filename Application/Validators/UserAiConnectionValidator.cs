using Application.Contracts.UserAiConnection;
using FluentValidation;

namespace Application.Validators
{
    public class UserAiConnectionValidator : AbstractValidator<UserAiConnectionDetailDto>
    {
        public UserAiConnectionValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Credentials).NotNull();

            // OAuth init: kaydı boş credential ile oluşturup "Connect" akışıyla tamamlayabiliriz.
            // Eğer doğrudan token verilecekse:
            // RuleFor(x => x.Credentials.ContainsKey("accessToken")).Equal(true).When(x => x.AuthType == AiAuthType.OAuth2);
        }
    }
}
