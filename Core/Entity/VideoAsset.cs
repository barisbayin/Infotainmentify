using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entity
{
    public class VideoAsset : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ScriptId { get; set; }

        [MaxLength(64)]
        public string AssetType { get; set; } = default!; // e.g. "image", "tts", "video", "thumbnail"

        [MaxLength(128)]
        public string AssetKey { get; set; } = default!; // e.g. "scene_1_image", "scene_3_tts"

        [MaxLength(256)]
        public string FilePath { get; set; } = default!; // user/renders/images/scene_1.jpg

        public bool IsGenerated { get; set; } = false;
        public bool IsUploaded { get; set; } = false;

        public DateTime? GeneratedAt { get; set; }
        public DateTime? UploadedAt { get; set; }

        // JSON for metadata (prompt, model, seed, etc.)
        public string? MetadataJson { get; set; }

        [ForeignKey(nameof(ScriptId))]
        public Script Script { get; set; } = default!;
    }
}
