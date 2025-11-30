using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    public class VideoPresetService : BaseService<VideoPreset>
    {
        public VideoPresetService(IRepository<VideoPreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }
    }
}
