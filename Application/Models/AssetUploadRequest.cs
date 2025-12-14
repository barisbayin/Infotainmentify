using Microsoft.AspNetCore.Http;

namespace Application.Models
{
    public class AssetUploadRequest
    {
        public IFormFile File { get; set; } = default!;
        public string Type { get; set; } = default!; // "music", "font", "branding"
    }
}
