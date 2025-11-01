using Application.Abstractions;
using Core.Abstractions;
using Core.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Cryptography;

namespace WebAPI.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif",
        ".mp4", ".mov", ".mkv",
        ".mp3", ".wav", ".m4a",
        ".pdf", ".txt", ".csv"
        // ihtiyaca göre ekle
    };

        //private const long MaxBytes = 200_000_000; // 200MB - [RequestSizeLimit] ile de korundu

        //private readonly InfotainmentifyDbContext _db;
        //private readonly IUserDirectoryService _dirs;
        //private readonly ICurrentUserService _current;

        //public FilesController(InfotainmentifyDbContext db, IUserDirectoryService dirs, ICurrentUserService current)
        //{ _db = db; _dirs = dirs; _current = current; }

        //public sealed record UploadResponse(string id, string fileName, string contentType, long size, string url, string sha256);

        //[HttpPost("upload")]
        //[RequestSizeLimit(MaxBytes)]
        //[ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
        //public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
        //{
        //    if (file is null || file.Length == 0)
        //        return BadRequest("Dosya yok veya boş.");

        //    if (file.Length > MaxBytes)
        //        return BadRequest($"Dosya çok büyük. Max {MaxBytes / (1024 * 1024)} MB.");

        //    // Current user & dizin hazırla
        //    var user = await _db.Set<AppUser>().FirstAsync(u => u.Id == _current.UserId, ct);
        //    await _dirs.EnsureUserScaffoldAsync(user, ct);

        //    // Klasör: /Users/{User}/files/uploads/yyyy/MM
        //    var uploadsRoot = _dirs.GetUserBaseFiles(user);
        //    var year = DateTime.Now.ToString("yyyy");
        //    var month = DateTime.Now.ToString("MM");
        //    var targetDir = Path.Combine(uploadsRoot, "uploads", year, month);
        //    Directory.CreateDirectory(targetDir);

        //    // Güvenli dosya adı & uzantı kontrolü
        //    var originalName = Path.GetFileName(file.FileName); // sadece isim
        //    var ext = Path.GetExtension(originalName);
        //    if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        //        return BadRequest($"Bu uzantıya izin yok: {ext}");

        //    // GUID tabanlı dosya adı (orijinal uzantı korunur)
        //    var id = Guid.NewGuid().ToString("N");
        //    var safeName = $"{id}{ext}";
        //    var targetPath = Path.Combine(targetDir, safeName);

        //    // Hash (akış üzerinde hesapla) + yaz
        //    string sha256Hex;
        //    await using (var input = file.OpenReadStream())
        //    await using (var fs = System.IO.File.Create(targetPath, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
        //    using (var sha = SHA256.Create())
        //    {
        //        // aynı anda hem diske yaz hem hashle
        //        var buffer = new byte[81920];
        //        int read;
        //        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        //        {
        //            await fs.WriteAsync(buffer.AsMemory(0, read), ct);
        //            sha.TransformBlock(buffer, 0, read, null, 0);
        //        }
        //        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        //        sha256Hex = Convert.ToHexString(sha.Hash!);
        //    }

        //    // İç sistem yolu sızdırma: URL üret (ör. /u/{user.DirectoryName}/files/uploads/yyyy/MM/{id}{ext})
        //    // _dirs’te public URL türeten bir yardımcı varsa onu kullan:
        //    var publicUrl = _dirs.GetPublicFileUrl(user, Path.Combine("uploads", year, month, safeName).Replace('\\', '/'));

        //    // Opsiyonel: DB’de bir Files tablosuna kaydet (örnek):
        //    // var entity = new MediaFile { Id = id, UserId = user.Id, OriginalName = originalName, Path = targetPath, Url = publicUrl, Sha256 = sha256Hex, Size = file.Length, ContentType = file.ContentType, CreatedAt = DateTime.Now };
        //    // _db.Add(entity); await _db.SaveChangesAsync(ct);

        //    // İçerik türü normalize et
        //    var provider = new FileExtensionContentTypeProvider();
        //    if (!provider.TryGetContentType(ext, out var normalizedContentType))
        //        normalizedContentType = "application/octet-stream";

        //    var resp = new UploadResponse(
        //        id: id,
        //        fileName: originalName,
        //        contentType: normalizedContentType,
        //        size: file.Length,
        //        url: publicUrl,
        //        sha256: sha256Hex.ToLowerInvariant()
        //    );

        //    return Ok(resp);
        //}

        //// İstersen çoklu yükleme:
        //[HttpPost("upload-many")]
        //[RequestSizeLimit(MaxBytes * 5)]
        //[ProducesResponseType(typeof(List<UploadResponse>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> UploadMany([FromForm] List<IFormFile> files, CancellationToken ct)
        //{
        //    if (files is null || files.Count == 0) return BadRequest("Dosya yok.");
        //    var results = new List<UploadResponse>(files.Count);
        //    foreach (var f in files)
        //    {
        //        // Tekli metodu tekrar kullanmak yerine küçük bir iç fonksiyonla aynı kontrolleri uygula
        //        HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), new FormFileCollection { f });
        //        var res = await Upload(f, ct) as OkObjectResult;
        //        if (res?.Value is UploadResponse item) results.Add(item);
        //    }
        //    return Ok(results);
        //}
    }
}
