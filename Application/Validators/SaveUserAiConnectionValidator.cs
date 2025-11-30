using Application.Contracts.AppUser;
using Core.Enums;
using FluentValidation;

namespace Application.Validators
{
    public class SaveUserAiConnectionValidator : AbstractValidator<SaveUserAiConnectionDto>
    {
        public SaveUserAiConnectionValidator()
        {
            // 1. Temel Kontroller
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Bağlantı adı boş olamaz.")
                .MaximumLength(100).WithMessage("Bağlantı adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.Provider)
                .IsEnumName(typeof(AiProviderType), caseSensitive: false)
                .WithMessage("Geçersiz AI sağlayıcısı.");

            RuleFor(x => x.ApiKey)
                .NotEmpty().WithMessage("API Anahtarı zorunludur.");

            // 2. PROVIDER BAZLI AKILLI KONTROLLER 🧠

            // OpenAI Kuralları
            When(x => x.Provider == AiProviderType.OpenAI.ToString(), () =>
            {
                RuleFor(x => x.ApiKey)
                    .Must(key => key.StartsWith("sk-"))
                    .WithMessage("OpenAI API anahtarları genellikle 'sk-' ile başlar. Yanlış anahtarı kopyalamış olabilir misiniz?");
            });

            // Google Vertex Kuralları (JSON olmalı)
            When(x => x.Provider == AiProviderType.GoogleVertex.ToString(), () =>
            {
                RuleFor(x => x.ApiKey)
                    .Must(BeValidJson)
                    .WithMessage("Google Vertex için 'Service Account JSON' içeriğinin tamamını yapıştırmalısınız.");

                // Google için Project ID genelde JSON içinden çıkar ama ExtraId doluysa kontrol et
                RuleFor(x => x.ExtraId)
                    .NotEmpty()
                    .When(x => !IsJsonContainsProjectId(x.ApiKey))
                    .WithMessage("JSON içinde project_id bulunamadı, lütfen ExtraId alanına Project ID giriniz.");
            });
        }

        // Helper: Basit JSON kontrolü
        private bool BeValidJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            return (value.StartsWith("{") && value.EndsWith("}")) ||
                   (value.StartsWith("[") && value.EndsWith("]"));
        }

        private bool IsJsonContainsProjectId(string json)
        {
            return json.Contains("\"project_id\"");
        }
    }
}
