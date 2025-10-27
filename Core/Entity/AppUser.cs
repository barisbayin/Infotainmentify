using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Entity
{
    public class AppUser : BaseEntity
    {
        [Required, MaxLength(256)]
        public string Email { get; set; } = default!;

        [Required, MaxLength(128)]
        public string Username { get; set; } = default!; // klasör adında kullanılacak

        [Required, MaxLength(512)]
        public string PasswordHash { get; set; } = default!;

        public UserRole Role { get; set; } = UserRole.Normal;

        // Hesaplanan alan: "userID_Username" (Id atandıktan sonra dolduracağız)
        [Required, MaxLength(256)]
        public string DirectoryName { get; set; } = default!; // örn: "42_baris"
    }
}
