using Application.Contracts.AppUser;
using Core.Entity;

namespace Application.Mappers
{
    public static class AppUserMappers
    {
        public static AppUserListDto ToListDto(this AppUser u) => new()
        {
            Id = u.Id,
            Email = u.Email,
            Username = u.Username,
            Role = u.Role.ToString(),
            Active = !u.IsActive
        };

        public static AppUserDetailDto ToDetailDto(this AppUser u) => new()
        {
            Id = u.Id,
            Email = u.Email,
            Username = u.Username,
            Role = u.Role.ToString(),
            DirectoryName = u.DirectoryName,
            Active = !u.IsActive
        };
    }
}
