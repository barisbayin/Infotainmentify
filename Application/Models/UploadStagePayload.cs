namespace Application.Models
{
    public class UploadStagePayload
    {
        // Tüm yüklemelerin listesi burada duracak
        public List<UploadResultItem> Uploads { get; set; } = new();

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
