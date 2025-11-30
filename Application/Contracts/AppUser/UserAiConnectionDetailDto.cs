namespace Application.Contracts.AppUser
{
    public class UserAiConnectionDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Provider { get; set; } = default!;

        // Detayda API Key'i asla tam dönmeyiz! Maskelenmiş döneriz.
        public string MaskedApiKey { get; set; } = default!;
        public string? ExtraId { get; set; } // Project ID vs.
    }
}
