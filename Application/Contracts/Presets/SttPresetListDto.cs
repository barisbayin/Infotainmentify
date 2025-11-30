namespace Application.Contracts.Presets
{
    public class SttPresetListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ModelName { get; set; } = default!;
        public string LanguageCode { get; set; } = default!;
        public DateTime? UpdatedAt { get; set; }
    }
}
