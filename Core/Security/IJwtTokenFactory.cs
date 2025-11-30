using Core.Entity.User;

namespace Core.Security
{
    public interface IJwtTokenFactory
    {
        string CreateToken(AppUser user);
    }
}
