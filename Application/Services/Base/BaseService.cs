using Core.Contracts;
using Core.Entity;

namespace Application.Services.Base
{
    public abstract class BaseService<T> : IBaseService<T> where T : BaseEntity
    {
        protected readonly IRepository<T> _repo;
        protected readonly IUnitOfWork _uow;

        protected BaseService(IRepository<T> repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public virtual async Task<T?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, ct);

            // SECURITY CHECK: Veri var mı ve sahibi bu kullanıcı mı?
            if (entity == null) return null;

            // Eğer entity'de AppUserId varsa kontrol et (Reflection veya Interface ile yapılabilir)
            // Performans için burada dynamic veya ortak interface (IOwnedEntity) kullanılabilir.
            // Basitlik adına burada Reflection örneği veriyorum:
            var prop = typeof(T).GetProperty("AppUserId");
            if (prop != null)
            {
                var ownerId = (int)prop.GetValue(entity)!;
                if (ownerId != userId) return null; // Veya throw SecurityException
            }

            return entity;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(int userId, CancellationToken ct = default)
        {
            // AppUserId'si eşleşenleri getir
            // Not: Burası Generic Repository'de dinamik Expression tree ile daha şık yapılır 
            // ama şimdilik mantığı anlaman için basit tutuyorum.
            // Gerçek projede: _repo.FindAsync(e => e.AppUserId == userId);

            // Geçici çözüm (Reflection ile filtre):
            // Doğrusu IRepository'e "GetByUserId" eklemektir.
            var all = await _repo.GetAllAsync(true, ct);
            return all.Where(x => IsOwner(x, userId));
        }

        public virtual async Task<T> AddAsync(T entity, int userId, CancellationToken ct = default)
        {
            // Kullanıcıyı set et
            var prop = typeof(T).GetProperty("AppUserId");
            if (prop != null) prop.SetValue(entity, userId);

            entity.CreatedAt = DateTime.UtcNow;

            await _repo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity;
        }

        public virtual async Task UpdateAsync(T entity, int userId, CancellationToken ct = default)
        {
            // Önce DB'den orijinali çekip userId kontrolü yapmak şarttır!
            var original = await GetByIdAsync(entity.Id, userId, ct);
            if (original == null) throw new UnauthorizedAccessException("Erişim reddedildi veya kayıt yok.");

            // Update
            entity.UpdatedAt = DateTime.UtcNow;
            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);
        }

        public virtual async Task DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            var entity = await GetByIdAsync(id, userId, ct);
            if (entity == null) throw new UnauthorizedAccessException("Erişim reddedildi veya kayıt yok.");

            _repo.Delete(entity); // Soft delete ise repository halleder
            await _uow.SaveChangesAsync(ct);
        }

        // Helper
        private bool IsOwner(T entity, int userId)
        {
            var prop = typeof(T).GetProperty("AppUserId");
            if (prop == null) return true; // Sahibi yoksa herkese açık
            return (int)prop.GetValue(entity)! == userId;
        }
    }
}
