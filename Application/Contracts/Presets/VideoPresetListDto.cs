namespace Application.Contracts.Presets
{
    // LIST
    public class VideoPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string GenerationMode { get; set; } = default!; // "ImageToVideo"
        public DateTime? UpdatedAt { get; set; }
    }
}
