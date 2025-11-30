namespace Application.Contracts.Presets
{
    // DETAIL
    public class ImagePresetDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int UserAiConnectionId { get; set; }

        public string ModelName { get; set; } = default!;
        public string? ArtStyle { get; set; }
        public string Size { get; set; } = default!;
        public string Quality { get; set; } = "standard";

        public string PromptTemplate { get; set; } = default!;
        public string? NegativePrompt { get; set; }
        public int ImageCountPerScene { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
