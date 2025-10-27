namespace Application.Contracts.AppUser
{
    public sealed class AppUserDetailDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string? DirectoryName { get; set; }
        public bool Active { get; set; }
    }
}
