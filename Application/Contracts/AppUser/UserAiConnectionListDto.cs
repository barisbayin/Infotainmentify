namespace Application.Contracts.AppUser
{
    public class UserAiConnectionListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Provider { get; set; } = default!; // "OpenAI"
        public DateTime CreatedAt { get; set; }
    }
}
