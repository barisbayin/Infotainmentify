namespace Application.Contracts.Presets
{
    // LIST (Grid için hafif)
    public class ScriptPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string Tone { get; set; } = default!;
        public string Language { get; set; } = default!;
        public DateTime? UpdatedAt { get; set; }
    }
}

