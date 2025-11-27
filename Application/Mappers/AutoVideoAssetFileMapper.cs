using Application.Contracts.AutoVideoAsset;
using Core.Entity;
using System.Text.Json;

namespace Application.Mappers
{
    public static class AutoVideoAssetFileMapper
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ---------------- LIST ----------------
        public static AutoVideoAssetFileListDto ToListDto(this AutoVideoAssetFile e)
        {
            return new AutoVideoAssetFileListDto
            {
                Id = e.Id,
                SceneNumber = e.SceneNumber,
                FilePath = e.FilePath,
                FileType = e.FileType.ToString(),
                IsGenerated = e.IsGenerated
            };
        }

        // ---------------- DETAIL ----------------
        public static AutoVideoAssetFileDetailDto ToDetailDto(this AutoVideoAssetFile e)
        {
            Dictionary<string, object>? meta = null;

            if (!string.IsNullOrWhiteSpace(e.MetadataJson))
            {
                try
                {
                    meta = JsonSerializer.Deserialize<Dictionary<string, object>>(e.MetadataJson!, _jsonOptions);
                }
                catch
                {
                    meta = null;
                }
            }

            return new AutoVideoAssetFileDetailDto
            {
                Id = e.Id,
                SceneNumber = e.SceneNumber,
                FilePath = e.FilePath,
                FileType = e.FileType.ToString(),
                AssetKey = e.AssetKey,
                IsGenerated = e.IsGenerated,
                Metadata = meta
            };
        }
    }
}
