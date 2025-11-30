using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.AppUser
{
    public class SaveUserAiConnectionDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public string Provider { get; set; } = default!; // Enum string olarak gelir

        [Required]
        public string ApiKey { get; set; } = default!; // Raw API Key (sk-...)

        public string? ExtraId { get; set; } // Google Project ID vs.
    }
}
