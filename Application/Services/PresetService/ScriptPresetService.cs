using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    public class ScriptPresetService : BaseService<ScriptPreset>
    {
        public ScriptPresetService(IRepository<ScriptPreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }

        // İleride "Bu kullanıcının varsayılan Script ayarını getir" gibi metodlar gerekirse buraya yazarız.
    }
}
