using System.ComponentModel.DataAnnotations;

namespace Application.Contracts.Asset
{

    // Yükleme İsteği (Controller parametresi olarak)
    public class AssetUploadDto
    {
        [Required]
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = default!;

        [Required]
        public string Type { get; set; } = default!; // "Music", "Font", "Branding"
    }
}
