namespace Application.Models
{
    public class StageResult
    {
        public bool Success { get; set; }
        public object? Output { get; set; }
        public string? Error { get; set; }
    }
}
