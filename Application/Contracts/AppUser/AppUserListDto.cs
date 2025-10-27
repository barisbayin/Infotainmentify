namespace Application.Contracts.AppUser
{
    public sealed class AppUserListDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Role { get; set; } = default!;
        public bool Active { get; set; }
    }

}
