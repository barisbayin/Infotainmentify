namespace Application.Contracts.Auth
{
    public sealed class AuthResponseDto
    {
        public string Token { get; set; } = default!;
        public AuthUserDto User { get; set; } = default!;
    }
}
