namespace Core.Abstractions
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string? Email { get; }
        string? Role { get; }
    }
}
