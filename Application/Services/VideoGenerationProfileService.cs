using Core.Contracts;
using Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.AutoVideoAsset;
using Application.Mappers;

namespace Application.Services
{
    public class VideoGenerationProfileService
    {
        private readonly IRepository<VideoGenerationProfile> _repo;
        private readonly IUnitOfWork _uow;

        public VideoGenerationProfileService(
            IRepository<VideoGenerationProfile> repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        // ------------------------------------------------------------
        // LIST (Current User)
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<VideoGenerationProfileListDto>> ListAsync(
            int userId,
            CancellationToken ct)
        {
            var list = await _repo.FindAsync(
                p => p.AppUserId == userId,
                asNoTracking: true,
                ct: ct,
                include: q => q
                    .Include(x => x.ScriptGenerationProfile)
                    .Include(x => x.SocialChannel)
            );

            return list.Select(x => x.ToListDto()).ToList();
        }

        // ------------------------------------------------------------
        // GET (Detail)
        // ------------------------------------------------------------
        public async Task<VideoGenerationProfileDetailDto?> GetAsync(
            int userId,
            int id,
            CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                include: q => q
                    .Include(x => x.ScriptGenerationProfile)
                    .Include(x => x.SocialChannel),
                asNoTracking: true,
                ct: ct
            );

            return entity?.ToDetailDto();
        }

        // ------------------------------------------------------------
        // UPSERT
        // ------------------------------------------------------------
        public async Task<int> UpsertAsync(
            int userId,
            VideoGenerationProfileDetailDto dto,
            CancellationToken ct)
        {
            var profileName = dto.ProfileName.Trim();

            // ---------- CREATE ----------
            if (dto.Id == 0)
            {
                var exists = await _repo.AnyAsync(
                    p => p.AppUserId == userId && p.ProfileName == profileName,
                    ct);

                if (exists)
                    throw new InvalidOperationException("Bu profil adı zaten kullanılıyor.");

                var e = new VideoGenerationProfile
                {
                    AppUserId = userId,
                    ProfileName = profileName,
                    ScriptGenerationProfileId = dto.ScriptGenerationProfileId,
                    SocialChannelId = dto.SocialChannelId,
                    UploadAfterRender = dto.UploadAfterRender,
                    GenerateThumbnail = dto.GenerateThumbnail,
                    TitleTemplate = dto.TitleTemplate,
                    DescriptionTemplate = dto.DescriptionTemplate,
                    IsActive = true
                };

                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }

            // ---------- UPDATE ----------
            var entity = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == dto.Id,
                asNoTracking: false,
                ct: ct
            );

            if (entity == null)
                throw new KeyNotFoundException("VideoGenerationProfile bulunamadı.");

            entity.ProfileName = profileName;
            entity.ScriptGenerationProfileId = dto.ScriptGenerationProfileId;
            entity.SocialChannelId = dto.SocialChannelId;
            entity.UploadAfterRender = dto.UploadAfterRender;
            entity.GenerateThumbnail = dto.GenerateThumbnail;
            entity.TitleTemplate = dto.TitleTemplate;
            entity.DescriptionTemplate = dto.DescriptionTemplate;
            entity.IsActive = dto.IsActive;

            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return entity.Id;
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------
        public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
        {
            var e = await _repo.FirstOrDefaultAsync(
                p => p.AppUserId == userId && p.Id == id,
                asNoTracking: false,
                ct: ct);

            if (e == null)
                return false;

            _repo.Delete(e);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
