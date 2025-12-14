using Core.Entity.User;
using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class AssetFile : BaseEntity
    {
        [Required]
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        // Kullanıcının gördüğü isim: "Benim Logom.png"
        [Required, MaxLength(255)]
        public string FriendlyName { get; set; } = default!;

        // Diskteki gerçek isim: "branding_guid_123.png"
        [Required, MaxLength(255)]
        public string PhysicalFileName { get; set; } = default!;

        public AssetType Type { get; set; }

        public long SizeInBytes { get; set; }

        // Müzik için süre (saniye), Font/Resim için null olabilir
        public double? DurationSec { get; set; }

        public string ContentType { get; set; } = "application/octet-stream"; // "audio/mpeg", "image/png"
    }
}
