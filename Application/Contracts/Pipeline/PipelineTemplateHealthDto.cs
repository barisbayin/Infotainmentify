namespace Application.Contracts.Pipeline
{
    public class PipelineTemplateHealthDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = default!;
        public string ProductionProfile { get; set; } = "Generic";
        public string Status { get; set; } = "Unknown";
        public bool IsRunnable { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public List<PipelineTemplateHealthStageDto> Stages { get; set; } = new();
        public List<PipelineTemplateHealthItemDto> Items { get; set; } = new();
        public List<string> RecommendedNextSteps { get; set; } = new();
    }

    public class PipelineTemplateHealthStageDto
    {
        public int StageConfigId { get; set; }
        public int Order { get; set; }
        public string StageType { get; set; } = default!;
        public int? PresetId { get; set; }
        public string? PresetName { get; set; }
        public string? PresetEntityType { get; set; }
        public string? ExecutorName { get; set; }
        public string Severity { get; set; } = "Info";
        public int? OutputWidth { get; set; }
        public int? OutputHeight { get; set; }
        public int? Fps { get; set; }
        public string? AspectRatio { get; set; }
        public int? TargetDurationSec { get; set; }
        public string? ImageSize { get; set; }
        public List<string> RequiredInputs { get; set; } = new();
        public List<string> SatisfiedInputs { get; set; } = new();
        public List<PipelineTemplateHealthItemDto> Issues { get; set; } = new();
        public List<PipelineTemplateHealthUploadTargetDto> UploadTargets { get; set; } = new();
    }

    public class PipelineTemplateHealthItemDto
    {
        public string Severity { get; set; } = "Info";
        public string Code { get; set; } = default!;
        public string Message { get; set; } = default!;
        public int? StageOrder { get; set; }
        public string? StageType { get; set; }
        public string? Details { get; set; }
    }

    public class PipelineTemplateHealthUploadTargetDto
    {
        public int SocialChannelId { get; set; }
        public string? ChannelName { get; set; }
        public string? ChannelType { get; set; }
        public string Severity { get; set; } = "Info";
        public string? Message { get; set; }
    }
}
