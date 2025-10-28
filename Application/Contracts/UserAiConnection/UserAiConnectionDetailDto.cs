namespace Application.Contracts.UserAiConnection
{
    public class UserAiConnectionDetailDto : UserAiConnectionListDto
    {
        public Dictionary<string, string>? Credentials { get; set; }
    }
}
