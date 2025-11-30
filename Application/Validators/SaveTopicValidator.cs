using Application.Contracts.Prompts;
using Application.Contracts.Topics;
using FluentValidation;
using System.Text.Json;

namespace Application.Validators
{
    // Validator artık 'SaveTopicDto'yu denetliyor
    public class SaveTopicValidator : AbstractValidator<SaveTopicDto>
    {
        public SaveTopicValidator()
        {
            // 1. Zorunlu Alanlar
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Başlık (Title) zorunludur.")
                .MaximumLength(200).WithMessage("Başlık 200 karakteri geçemez.");

            RuleFor(x => x.Premise)
                .NotEmpty().WithMessage("Ana fikir (Premise) zorunludur."); // Veritabanında NVARCHAR(MAX) olabilir ama boş olamaz.

            // 2. Format Kontrolleri
            RuleFor(x => x.LanguageCode)
                .NotEmpty()
                .MaximumLength(10).WithMessage("Dil kodu en fazla 10 karakter olabilir (örn: tr-TR).");

            // 3. Uzunluk Sınırları (Database şemasına uygun)
            RuleFor(x => x.Category).MaximumLength(64);
            RuleFor(x => x.SubCategory).MaximumLength(128);
            RuleFor(x => x.Series).MaximumLength(128);

            RuleFor(x => x.Tone).MaximumLength(64);
            RuleFor(x => x.RenderStyle).MaximumLength(64);
            RuleFor(x => x.VisualPromptHint).MaximumLength(256);

            // 4. JSON Validasyonu (TagsJson için)
            // Eğer doluysa, geçerli bir JSON array olup olmadığına bakıyoruz
            RuleFor(x => x.TagsJson)
                .Must(BeValidJson)
                .When(x => !string.IsNullOrWhiteSpace(x.TagsJson))
                .WithMessage("Etiketler (Tags) geçerli bir JSON formatında olmalıdır.");
        }

        // Helper: JSON kontrolü
        private bool BeValidJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return true; // Boşsa valid kabul et (Nullable çünkü)
            try
            {
                using var doc = JsonDocument.Parse(json);
                // Ekstra kontrol: TagsJson bir Array olmalı ["tag1", "tag2"]
                return doc.RootElement.ValueKind == JsonValueKind.Array;
            }
            catch
            {
                return false;
            }
        }
    }
}
