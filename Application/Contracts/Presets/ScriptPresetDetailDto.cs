namespace Application.Contracts.Presets
{
    // DETAIL (Form doldurmak için tam veri)
    public class ScriptPresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int UserAiConnectionId { get; set; }

        public string ModelName { get; set; } = default!;
        public string Tone { get; set; } = default!;
        public int TargetDurationSec { get; set; }
        public string Language { get; set; } = default!;

        public bool IncludeHook { get; set; }
        public bool IncludeCta { get; set; }

        public string PromptTemplate { get; set; } = default!;
        public string? SystemInstruction { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

