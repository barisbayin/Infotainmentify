namespace Application.Contracts.UserAiConnection
{
    public class UserAiConnectionListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Provider { get; set; } = null!;
        public string? TextModel { get; set; }
        public string? ImageModel { get; set; }
        public string? VideoModel { get; set; }
        public decimal? Temperature { get; set; }
    }
}
