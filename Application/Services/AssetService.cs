using Application.Contracts.Asset;
using Application.Services.Interfaces;
using Core.Contracts;
using Core.Entity;
using Core.Enums;

namespace Application.Services
{
    public class AssetService : IAssetService
    {
        private readonly IRepository<AssetFile> _assetRepo;
        private readonly IUnitOfWork _uow;
        private readonly string _assetsRootPath;

        public AssetService(IRepository<AssetFile> assetRepo, IUnitOfWork uow)
        {
            _assetRepo = assetRepo;
            _uow = uow;
            // Fiziksel yol: .../ALL_FILES/Assets
            _assetsRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES", "Assets");
        }

        public async Task<List<AssetListDto>> GetUserAssetsAsync(int userId, AssetType type)
        {
            var entities = await _assetRepo.FindAsync(
                predicate: x => x.AppUserId == userId && x.Type == type,
                orderBy: x => x.CreatedAt,
                desc: true,
                asNoTracking: true
            );

            // Mapping ve Formatlama BURADA yapılır
            return entities.Select(x => new AssetListDto
            {
                Id = x.Id,
                Name = x.FriendlyName,
                Type = x.Type.ToString(),
                Url = GetPublicUrl(x.Type, x.PhysicalFileName),
                SizeInfo = FormatSize(x.SizeInBytes)
            }).ToList();
        }

        public async Task<AssetDetailDto> UploadFileAsync(int userId, AssetUploadDto input)
        {
            if (input.File == null || input.File.Length == 0)
                throw new ArgumentException("Dosya boş.");

            // Enum Parse
            if (!Enum.TryParse<AssetType>(input.Type, true, out var typeEnum))
                throw new ArgumentException("Geçersiz asset türü.");

            // 1. Klasör İşlemleri
            var folderName = GetFolderName(typeEnum);
            var uploadPath = Path.Combine(_assetsRootPath, folderName);
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var ext = Path.GetExtension(input.File.FileName).ToLower();
            var uniqueName = $"{typeEnum}_{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadPath, uniqueName);

            // 2. Kaydet
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await input.File.CopyToAsync(stream);
            }

            // 3. Entity Oluştur
            var asset = new AssetFile
            {
                AppUserId = userId,
                FriendlyName = input.File.FileName,
                PhysicalFileName = uniqueName,
                Type = typeEnum,
                SizeInBytes = input.File.Length,
                ContentType = input.File.ContentType,
                CreatedAt = DateTime.UtcNow
            };

            await _assetRepo.AddAsync(asset);
            await _uow.SaveChangesAsync();

            // 4. Return DTO (Mapping BURADA)
            return new AssetDetailDto
            {
                Id = asset.Id,
                Name = asset.FriendlyName,
                PhysicalName = asset.PhysicalFileName,
                Type = asset.Type.ToString(),
                Url = GetPublicUrl(asset.Type, asset.PhysicalFileName),
                SizeInfo = FormatSize(asset.SizeInBytes),
                DurationSec = asset.DurationSec,
                CreatedAt = asset.CreatedAt
            };
        }

        public async Task<bool> DeleteFileAsync(int userId, int assetId)
        {
            var asset = await _assetRepo.FirstOrDefaultAsync(x => x.Id == assetId && x.AppUserId == userId);
            if (asset == null) return false;

            var folderName = GetFolderName(asset.Type);
            var fullPath = Path.Combine(_assetsRootPath, folderName, asset.PhysicalFileName);

            if (File.Exists(fullPath)) File.Delete(fullPath);

            _assetRepo.Delete(asset);
            await _uow.SaveChangesAsync();

            return true;
        }

        // --- PRIVATE BUSINESS LOGIC HELPERS ---

        // URL Üretici: /files/Assets/music/dosya.mp3
        private string GetPublicUrl(AssetType type, string fileName)
        {
            var folder = GetFolderName(type);
            return $"/Assets/{folder}/{fileName}";
        }

        // Klasör Adı Eşleştirici
        private string GetFolderName(AssetType type) => type switch
        {
            AssetType.Music => "music",
            AssetType.Font => "fonts",
            AssetType.Branding => "branding",
            _ => "others"
        };

        // Boyut Formatlayıcı (Byte -> MB/KB)
        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
}
