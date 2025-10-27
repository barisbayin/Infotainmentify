using Core.Entity;

namespace Core.Security
{
    public interface IJwtTokenFactory
    {
        string CreateToken(AppUser user);
    }
}
