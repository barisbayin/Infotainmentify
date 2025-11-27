using Core.Enums;

namespace Application.Contracts.Render
{
    public class RenderProfileListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Resolution { get; set; } = default!;
        public int Fps { get; set; }
        public string? Style { get; set; }
        public CaptionPositionTypes CaptionPosition { get; set; }
    }
}
