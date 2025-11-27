namespace Application.Contracts.UserAiConnection
{
    public class UserAiConnectionDetailDto : UserAiConnectionListDto
    {
        public string? CredentialFilePath { get; set; }
        public string? Credentials { get; set; }   // ← STRING OLMALI
    }
}
