namespace Application.Contracts.Presets
{
    // LIST
    public class RenderPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public int Fps { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
