namespace Application.Options
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Key { get; set; } = default!;          // Symmetric key (HMAC)
        public int ExpiresDays { get; set; } = 7;            // Token ömrü (gün)
    }
}
