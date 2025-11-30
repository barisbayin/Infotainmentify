namespace Application.Contracts.Presets
{
    public class ImagePresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string Size { get; set; } = default!; // "1024x1792"
        public DateTime? UpdatedAt { get; set; }
    }
}
