using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    public class SttPresetService : BaseService<SttPreset>
    {
        public SttPresetService(IRepository<SttPreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }
    }
}
