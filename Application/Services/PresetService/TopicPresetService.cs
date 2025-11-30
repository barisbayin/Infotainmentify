using Application.Services.Base;
using Core.Contracts;
using Core.Entity.Presets;

namespace Application.Services.PresetService
{
    // Namespace düzeltmesi: Application.Services altında olması daha standarttır
    public class TopicPresetService : BaseService<TopicPreset>
    {
        public TopicPresetService(IRepository<TopicPreset> repo, IUnitOfWork uow) : base(repo, uow)
        {
        }

        // Buraya özel iş kuralları (Business Logic) gelirse ekleriz.
        // Şimdilik BaseService tüm CRUD işini görüyor.
    }
}
