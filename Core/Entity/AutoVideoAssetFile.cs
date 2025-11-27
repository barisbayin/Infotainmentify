using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class AutoVideoAssetFile : BaseEntity
    {
        public int AppUserId { get; set; }
        public AppUser User { get; set; } = null!;

        public int AutoVideoPipelineId { get; set; }
        public ContentPipelineRun Pipeline { get; set; } = null!;

        // Hangi sahneye ait
        public int SceneNumber { get; set; }

        // Dosya yolu
        [MaxLength(500)]
        public string FilePath { get; set; } = null!;

        // image, audio, video, final...
        public AutoVideoAssetFileType FileType { get; set; }

        // Örn: "scene_001_audio", "scene_003_image"
        [MaxLength(100)]
        public string? AssetKey { get; set; }

        // Sistem tarafından mı üretildi?
        public bool IsGenerated { get; set; } = true;

        // Thumbnail, duration vb.
        public string? MetadataJson { get; set; }

    }
}
