using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    public class RenderPresetService : BaseService<RenderPreset>
    {
        public RenderPresetService(IRepository<RenderPreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }
    }
}
