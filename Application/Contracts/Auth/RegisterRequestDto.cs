namespace Application.Contracts.Auth
{
    public sealed class RegisterRequestDto
    {
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
