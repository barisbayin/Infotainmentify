using Core.Entity;

namespace Application.Services.Base
{
    public interface IBaseService<T> where T : BaseEntity
    {
        // Güvenli Getir (Sadece sahibine)
        Task<T?> GetByIdAsync(int id, int userId, CancellationToken ct = default);

        // Listele (Filtreli ve Sayfalı)
        Task<IEnumerable<T>> GetAllAsync(int userId, CancellationToken ct = default);

        // Ekle
        Task<T> AddAsync(T entity, int userId, CancellationToken ct = default);

        // Güncelle
        Task UpdateAsync(T entity, int userId, CancellationToken ct = default);

        // Sil
        Task DeleteAsync(int id, int userId, CancellationToken ct = default);
    }
}
