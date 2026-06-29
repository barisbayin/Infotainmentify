using Application.Models;

namespace Application.Contracts.Pipeline
{
    // DETAIL RESPONSE (Polling için kullanılacak)
    public class PipelineRunDetailDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = default!;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FinalVideoUrl { get; set; }
        public int? FinalVideoWidth { get; set; }
        public int? FinalVideoHeight { get; set; }
        public string? FinalVideoAspectRatio { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? ThumbnailWidth { get; set; }
        public int? ThumbnailHeight { get; set; }
        public ProductionBrief? Brief { get; set; }
        public List<PipelineStageDto> Stages { get; set; } = new();
    }
}
