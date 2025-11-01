using Core.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public abstract class BaseEntity : ISoftDelete
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Soft delete kullanacaksan:
        public bool Removed { get; set; }
        public DateTime? RemovedAt { get; set; }
    }
}
