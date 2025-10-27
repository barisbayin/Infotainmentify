using Core.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public abstract class BaseEntity : ISoftDelete
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Soft delete kullanacaksan:
        public bool Removed { get; set; }
        public DateTime? RemovedAt { get; set; }
    }
}
