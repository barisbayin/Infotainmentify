using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    public class TtsPresetService : BaseService<TtsPreset>
    {
        public TtsPresetService(IRepository<TtsPreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }
    }
}
