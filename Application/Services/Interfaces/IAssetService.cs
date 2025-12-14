using Application.Contracts.Asset;
using Core.Enums;

namespace Application.Services.Interfaces
{
    public interface IAssetService
    {
        // Direkt ListDto döner
        Task<List<AssetListDto>> GetUserAssetsAsync(int userId, AssetType type);

        // Yükleme sonucu DetailDto döner
        Task<AssetDetailDto> UploadFileAsync(int userId, AssetUploadDto input);

        Task<bool> DeleteFileAsync(int userId, int assetId);
    }
}
