using Application.Contracts.Render;
using Application.Mappers;
using Core.Contracts;
using Core.Entity;

namespace Application.Services
{
    public class RenderProfileService
    {
        private readonly IRepository<AutoVideoRenderProfile> _repo;
        private readonly IUnitOfWork _uow;

        public RenderProfileService(
            IRepository<AutoVideoRenderProfile> repo,
            IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        // LIST
        public async Task<IReadOnlyList<RenderProfileListDto>> ListAsync(
            int userId,
            CancellationToken ct)
        {
            var items = await _repo.FindAsync(
                x => x.AppUserId == userId,
                asNoTracking: true,
                ct: ct
            );

            return items.Select(x => x.ToListDto()).ToList();
        }

        // GET
        public async Task<RenderProfileDetailDto?> GetAsync(
            int userId,
            int id,
            CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.AppUserId == userId && x.Id == id,
                asNoTracking: true,
                ct: ct);

            return entity?.ToDetailDto();
        }

        // UPSERT
        public async Task<int> UpsertAsync(
    int userId,
    RenderProfileDetailDto dto,
    CancellationToken ct)
        {
            dto.Name = dto.Name.Trim();

            // -----------------------------
            // CREATE
            // -----------------------------
            if (dto.Id == 0)
            {
                var exists = await _repo.AnyAsync(x =>
                    x.AppUserId == userId &&
                    x.Name == dto.Name,
                    ct);

                if (exists)
                    throw new InvalidOperationException("Bu isimde bir render profili zaten mevcut.");

                var e = new AutoVideoRenderProfile
                {
                    AppUserId = userId,
                    Name = dto.Name,

                    // RESOLUTION
                    Resolution = dto.Resolution,
                    Fps = dto.Fps,

                    // STYLE
                    Style = dto.Style,

                    // CAPTIONS
                    CaptionStyle = dto.CaptionStyle!,
                    CaptionFont = dto.CaptionFont!,
                    CaptionSize = dto.CaptionSize,
                    CaptionGlow = dto.CaptionGlow,
                    CaptionGlowColor = dto.CaptionGlowColor!,
                    CaptionGlowSize = dto.CaptionGlowSize,
                    CaptionOutlineSize = dto.CaptionOutlineSize,
                    CaptionShadowSize = dto.CaptionShadowSize,
                    CaptionKaraoke = dto.CaptionKaraoke,
                    CaptionHighlightColor = dto.CaptionHighlightColor!,
                    CaptionChunkSize = dto.CaptionChunkSize,
                    CaptionPosition = dto.CaptionPosition,
                    CaptionMarginV = dto.CaptionMarginV,
                    CaptionLineSpacing = dto.CaptionLineSpacing,
                    CaptionMaxWidthPercent = dto.CaptionMaxWidthPercent,
                    CaptionBackgroundOpacity = dto.CaptionBackgroundOpacity,
                    CaptionAnimation = dto.CaptionAnimation,

                    // MOTION
                    MotionIntensity = dto.MotionIntensity,
                    ZoomSpeed = dto.ZoomSpeed,
                    ZoomMax = dto.ZoomMax,
                    PanX = dto.PanX,
                    PanY = dto.PanY,

                    // TRANSITION
                    Transition = dto.Transition!,
                    TransitionDuration = dto.TransitionDuration,
                    TransitionDirection = dto.TransitionDirection!,
                    TransitionEasing = dto.TransitionEasing!,
                    TransitionStrength = dto.TransitionStrength,

                    // TIMELINE
                    TimelineMode = dto.TimelineMode!,

                    // AUDIO
                    BgmVolume = dto.BgmVolume,
                    VoiceVolume = dto.VoiceVolume,
                    DuckingStrength = dto.DuckingStrength,

                    // AI
                    AiRecommendedStyle = dto.AiRecommendedStyle,
                    AiRecommendedTransitions = dto.AiRecommendedTransitions,
                    AiRecommendedCaption = dto.AiRecommendedCaption
                };

                await _repo.AddAsync(e, ct);
                await _uow.SaveChangesAsync(ct);
                return e.Id;
            }

            // -----------------------------
            // UPDATE
            // -----------------------------
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.Id == dto.Id && x.AppUserId == userId,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                throw new KeyNotFoundException("RenderProfile bulunamadı.");

            entity.Name = dto.Name;

            // RESOLUTION
            entity.Resolution = dto.Resolution;
            entity.Fps = dto.Fps;

            // STYLE
            entity.Style = dto.Style;

            // CAPTIONS
            entity.CaptionStyle = dto.CaptionStyle!;
            entity.CaptionFont = dto.CaptionFont!;
            entity.CaptionSize = dto.CaptionSize;
            entity.CaptionGlow = dto.CaptionGlow;
            entity.CaptionGlowColor = dto.CaptionGlowColor!;
            entity.CaptionGlowSize = dto.CaptionGlowSize;
            entity.CaptionOutlineSize = dto.CaptionOutlineSize;
            entity.CaptionShadowSize = dto.CaptionShadowSize;
            entity.CaptionKaraoke = dto.CaptionKaraoke;
            entity.CaptionHighlightColor = dto.CaptionHighlightColor!;
            entity.CaptionChunkSize = dto.CaptionChunkSize;
            entity.CaptionPosition = dto.CaptionPosition;
            entity.CaptionMarginV = dto.CaptionMarginV;
            entity.CaptionLineSpacing = dto.CaptionLineSpacing;
            entity.CaptionMaxWidthPercent = dto.CaptionMaxWidthPercent;
            entity.CaptionBackgroundOpacity = dto.CaptionBackgroundOpacity;
            entity.CaptionAnimation = dto.CaptionAnimation;

            // MOTION
            entity.MotionIntensity = dto.MotionIntensity;
            entity.ZoomSpeed = dto.ZoomSpeed;
            entity.ZoomMax = dto.ZoomMax;
            entity.PanX = dto.PanX;
            entity.PanY = dto.PanY;

            // TRANSITION
            entity.Transition = dto.Transition!;
            entity.TransitionDuration = dto.TransitionDuration;
            entity.TransitionDirection = dto.TransitionDirection!;
            entity.TransitionEasing = dto.TransitionEasing!;
            entity.TransitionStrength = dto.TransitionStrength;

            // TIMELINE
            entity.TimelineMode = dto.TimelineMode!;

            // AUDIO
            entity.BgmVolume = dto.BgmVolume;
            entity.VoiceVolume = dto.VoiceVolume;
            entity.DuckingStrength = dto.DuckingStrength;

            // AI
            entity.AiRecommendedStyle = dto.AiRecommendedStyle;
            entity.AiRecommendedTransitions = dto.AiRecommendedTransitions;
            entity.AiRecommendedCaption = dto.AiRecommendedCaption;

            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return entity.Id;
        }


        // DELETE
        public async Task<bool> DeleteAsync(
            int userId,
            int id,
            CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                x => x.AppUserId == userId && x.Id == id,
                asNoTracking: false,
                ct: ct);

            if (entity == null)
                return false;

            _repo.Delete(entity);
            await _uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
