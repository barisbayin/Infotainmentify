using Application.Contracts.VideoAsset;
using Core.Entity;

namespace Application.Mappers
{
    public static class VideoAssetMapper
    {
        public static VideoAssetListDto ToListDto(VideoAsset e) => new()
        {
            Id = e.Id,
            ScriptId = e.ScriptId,
            AssetType = e.AssetType,
            AssetKey = e.AssetKey,
            FilePath = e.FilePath,
            IsGenerated = e.IsGenerated,
            IsUploaded = e.IsUploaded,
            GeneratedAt = e.GeneratedAt,
            UploadedAt = e.UploadedAt
        };

        public static VideoAssetDetailDto ToDetailDto(VideoAsset e) => new()
        {
            Id = e.Id,
            ScriptId = e.ScriptId,
            AssetType = e.AssetType,
            AssetKey = e.AssetKey,
            FilePath = e.FilePath,
            IsGenerated = e.IsGenerated,
            IsUploaded = e.IsUploaded,
            GeneratedAt = e.GeneratedAt,
            UploadedAt = e.UploadedAt,
            MetadataJson = e.MetadataJson
        };
    }
}
