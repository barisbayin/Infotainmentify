namespace Application.Contracts.Auth
{
    public sealed class AuthUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Role { get; set; } = default!;          // "Admin" | "Normal"
        public string DirectoryName { get; set; } = default!; // "1_baris" gibi
    }
}
