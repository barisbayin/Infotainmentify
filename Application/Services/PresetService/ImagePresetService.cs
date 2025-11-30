using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    public class ImagePresetService : BaseService<ImagePreset>
    {
        public ImagePresetService(IRepository<ImagePreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }
    }
}
