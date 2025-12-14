namespace Application.Contracts.Render
{
    public class RenderPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public int Fps { get; set; }
        public string EncoderPreset { get; set; } = default!;
        public DateTime? UpdatedAt { get; set; }
    }
}
